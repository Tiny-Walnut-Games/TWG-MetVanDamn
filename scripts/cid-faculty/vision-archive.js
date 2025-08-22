#!/usr/bin/env node
/**
 * CID Faculty - Vision Archive Manager
 * 
 * Manages the archival and indexing of Oracle Vision Reports.
 * Handles storage, cross-linking, and retrieval of vision data.
 */

const fs = require('fs');
const path = require('path');

class VisionArchive {
    constructor(archiveDir = 'docs/oracle_visions') {
        this.archiveDir = archiveDir;
        this.indexFile = path.join(archiveDir, 'index.json');
        this.templateFile = path.join(archiveDir, 'templates', 'vision-report-template.md');
        
        this.ensureArchiveStructure();
        this.index = this.loadIndex();
        
        console.log(`📚 Vision Archive initialized - ${this.index.visions?.length || 0} visions archived`);
    }

    ensureArchiveStructure() {
        // Ensure main directories exist
        const dirs = [
            this.archiveDir,
            path.join(this.archiveDir, 'templates'),
            path.join(this.archiveDir, new Date().getFullYear().toString()),
            path.join(this.archiveDir, new Date().getFullYear().toString(), 
                     String(new Date().getMonth() + 1).padStart(2, '0'))
        ];
        
        dirs.forEach(dir => {
            if (!fs.existsSync(dir)) {
                fs.mkdirSync(dir, { recursive: true });
            }
        });

        // Ensure index file exists
        if (!fs.existsSync(this.indexFile)) {
            const initialIndex = {
                metadata: {
                    created: new Date().toISOString(),
                    lastUpdated: new Date().toISOString(),
                    totalVisions: 0,
                    version: '1.0.0'
                },
                visions: []
            };
            fs.writeFileSync(this.indexFile, JSON.stringify(initialIndex, null, 2));
        }
    }

    loadIndex() {
        try {
            const data = JSON.parse(fs.readFileSync(this.indexFile, 'utf8'));
            return data;
        } catch (error) {
            console.error(`❌ Failed to load vision index: ${error.message}`);
            return { visions: [], metadata: {} };
        }
    }

    saveIndex() {
        try {
            this.index.metadata.lastUpdated = new Date().toISOString();
            this.index.metadata.totalVisions = this.index.visions.length;
            
            fs.writeFileSync(this.indexFile, JSON.stringify(this.index, null, 2));
            console.log(`💾 Vision index updated - ${this.index.visions.length} visions`);
        } catch (error) {
            console.error(`❌ Failed to save vision index: ${error.message}`);
        }
    }

    /**
     * Archive a vision report with full indexing
     */
    archiveVision(visionRequest, visionData, oracleReport) {
        const visionId = visionRequest.id;
        const timestamp = new Date();
        
        // Create file paths
        const year = timestamp.getFullYear().toString();
        const month = String(timestamp.getMonth() + 1).padStart(2, '0');
        const yearDir = path.join(this.archiveDir, year);
        const monthDir = path.join(yearDir, month);
        
        // Ensure month directory exists
        if (!fs.existsSync(monthDir)) {
            fs.mkdirSync(monthDir, { recursive: true });
        }
        
        const reportPath = path.join(monthDir, `${visionId}.md`);
        const dataPath = path.join(monthDir, `${visionId}.json`);
        
        // Generate vision report from template
        const reportContent = this.generateVisionReport(visionRequest, visionData, oracleReport);
        
        // Archive both markdown report and structured data
        fs.writeFileSync(reportPath, reportContent);
        fs.writeFileSync(dataPath, JSON.stringify({
            visionRequest,
            visionData,
            oracleReport,
            metadata: {
                archived: timestamp.toISOString(),
                reportPath: path.relative(this.archiveDir, reportPath),
                dataPath: path.relative(this.archiveDir, dataPath)
            }
        }, null, 2));
        
        // Update index
        const indexEntry = {
            id: visionId,
            timestamp: visionRequest.timestamp,
            archived: timestamp.toISOString(),
            sourceIntel: visionRequest.sourceIntel,
            trigger: visionRequest.trigger,
            visionType: visionRequest.visionType,
            priority: visionRequest.priority,
            disposition: 'pending', // Will be updated later
            reportPath: path.relative(this.archiveDir, reportPath),
            dataPath: path.relative(this.archiveDir, dataPath),
            resultingChanges: []
        };
        
        this.index.visions.push(indexEntry);
        this.saveIndex();
        
        console.log(`📚 Vision archived: ${visionId}`);
        return {
            reportPath: reportPath,
            dataPath: dataPath,
            indexEntry: indexEntry
        };
    }

    /**
     * Generate vision report from template
     */
    generateVisionReport(visionRequest, visionData, oracleReport) {
        let template;
        
        try {
            template = fs.readFileSync(this.templateFile, 'utf8');
        } catch (error) {
            console.warn(`⚠️  Template not found, using basic format`);
            return this.generateBasicReport(visionRequest, visionData, oracleReport);
        }

        // Extract Oracle insights from the report
        const visionSummary = this.extractVisionSummary(oracleReport);
        const recommendedSteps = this.extractRecommendedSteps(oracleReport);
        const loreHook = this.generateLoreHook(visionRequest, oracleReport);
        
        // Replace template variables
        const replacements = {
            '{{VISION_ID}}': visionRequest.id,
            '{{TIMESTAMP}}': new Date().toISOString(),
            '{{VISION_TYPE}}': visionRequest.visionType,
            '{{STATUS}}': 'archived',
            '{{TRIGGER}}': visionRequest.trigger,
            '{{TRIGGER_REASON}}': visionRequest.triggerReason,
            '{{SOURCE_INTEL_LINK}}': visionRequest.sourceIntel || 'N/A',
            '{{REQUESTED_BY}}': visionRequest.requestedBy,
            '{{PRIORITY}}': visionRequest.priority,
            '{{CONTEXT_NOTES}}': visionRequest.contextNotes,
            '{{VISION_SUMMARY}}': visionSummary,
            '{{ORACULAR_INSIGHTS}}': this.formatOracularInsights(oracleReport),
            '{{RECOMMENDED_STEPS}}': recommendedSteps,
            '{{LORE_HOOK}}': loreHook,
            '{{SCENARIO_ANALYSIS}}': this.formatScenarioAnalysis(oracleReport),
            '{{RISK_ASSESSMENT}}': this.formatRiskAssessment(oracleReport),
            '{{LEADING_INDICATORS}}': this.formatLeadingIndicators(oracleReport),
            '{{DISPOSITION}}': 'pending',
            '{{DISPOSITION_REASON}}': 'Awaiting Faculty review',
            '{{DISPOSITION_DATE}}': new Date().toISOString(),
            '{{RESULTING_CHANGES}}': '(To be updated)',
            '{{RELATED_VISIONS}}': '(To be determined)',
            '{{RESULTING_ISSUES}}': '(To be updated)',
            '{{RESULTING_PRS}}': '(To be updated)',
            '{{RELATED_TLDL}}': '(To be determined)'
        };
        
        let report = template;
        Object.entries(replacements).forEach(([placeholder, value]) => {
            report = report.replace(new RegExp(placeholder, 'g'), value);
        });
        
        return report;
    }

    extractVisionSummary(oracleReport) {
        if (!oracleReport || !oracleReport.summary) {
            return 'The Oracle\'s vision remains obscured by temporal mists.';
        }
        
        return oracleReport.summary.join('\n\n');
    }

    extractRecommendedSteps(oracleReport) {
        if (!oracleReport || !oracleReport.scenarios) {
            return '1. Consult the Oracle again with clearer context\n2. Gather additional intel from the Advisor\n3. Review source materials for insight';
        }
        
        const steps = [];
        oracleReport.scenarios.forEach((scenario, i) => {
            steps.push(`### Path ${i + 1}: ${scenario.name}`);
            steps.push(scenario.description);
            steps.push('');
            steps.push('**Prerequisites:**');
            scenario.prerequisites.forEach(prereq => {
                steps.push(`- ${prereq}`);
            });
            steps.push('');
        });
        
        return steps.join('\n');
    }

    generateLoreHook(visionRequest, oracleReport) {
        const triggerLore = {
            advisor: "The Advisor's wisdom speaks of patterns yet unseen, calling forth the Oracle's sight to peer beyond the veil of code.",
            intuition: "A whisper from the depths of faculty intuition beckons the Oracle to gaze into possibilities uncharted.",
            system: "The very systems cry out for guidance, their patterns forming sigils that only the Oracle can interpret.",
            manual: "A seeker approaches the Oracle's sanctum, bearing questions that demand the sight of futures potential."
        };
        
        const baseLore = triggerLore[visionRequest.trigger] || triggerLore.manual;
        
        if (oracleReport && oracleReport.scenarios && oracleReport.scenarios.length > 0) {
            const pathCount = oracleReport.scenarios.length;
            return `${baseLore}\n\nThrough the crystal ball of strategic foresight, ${pathCount} paths emerge from the mists of uncertainty. Each path bears its own trials and treasures, waiting for the bold to choose their destiny.`;
        }
        
        return `${baseLore}\n\nThe vision remains clouded, suggesting the need for deeper contemplation and clearer source material.`;
    }

    formatOracularInsights(oracleReport) {
        if (!oracleReport) return 'The Oracle\'s insights remain veiled.';
        
        const insights = [];
        
        if (oracleReport.metadata) {
            insights.push(`**Forecast Horizon:** ${oracleReport.metadata.forecastHorizon} months`);
            insights.push(`**Scenario Count:** ${oracleReport.metadata.scenarioCount}`);
            insights.push(`**High Probability Paths:** ${oracleReport.metadata.highProbability}`);
            insights.push(`**Transformative Potential:** ${oracleReport.metadata.transformativeScenarios > 0 ? 'Present' : 'Limited'}`);
        }
        
        return insights.join('\n');
    }

    formatScenarioAnalysis(oracleReport) {
        if (!oracleReport || !oracleReport.scenarios) {
            return 'No scenarios available for analysis.';
        }
        
        const analysis = [];
        oracleReport.scenarios.forEach((scenario, i) => {
            analysis.push(`**${i + 1}. ${scenario.name}** (${(scenario.probability * 100).toFixed(1)}% probability)`);
            analysis.push(`- Timeframe: ${scenario.timeframe}`);
            analysis.push(`- ${scenario.description}`);
            analysis.push('');
        });
        
        return analysis.join('\n');
    }

    formatRiskAssessment(oracleReport) {
        if (!oracleReport || !oracleReport.scenarios) {
            return 'Risk assessment unavailable.';
        }
        
        const risks = [];
        oracleReport.scenarios.forEach(scenario => {
            if (scenario.risks) {
                risks.push(`**${scenario.name} Risks:**`);
                scenario.risks.forEach(risk => {
                    risks.push(`- ${risk}`);
                });
                risks.push('');
            }
        });
        
        return risks.join('\n');
    }

    formatLeadingIndicators(oracleReport) {
        if (!oracleReport || !oracleReport.scenarios) {
            return 'No leading indicators identified.';
        }
        
        const indicators = [];
        oracleReport.scenarios.forEach(scenario => {
            if (scenario.leadingIndicators) {
                indicators.push(`**${scenario.name} Indicators:**`);
                scenario.leadingIndicators.forEach(indicator => {
                    indicators.push(`- ${indicator}`);
                });
                indicators.push('');
            }
        });
        
        return indicators.join('\n');
    }

    generateBasicReport(visionRequest, visionData, oracleReport) {
        return `# 🔮 Vision Report: ${visionRequest.id}

**Timestamp:** ${new Date().toISOString()}
**Trigger:** ${visionRequest.trigger} - ${visionRequest.triggerReason}
**Source:** ${visionRequest.sourceIntel || 'N/A'}

## Context
${visionRequest.contextNotes}

## Oracle's Vision
${oracleReport ? JSON.stringify(oracleReport, null, 2) : 'Vision data unavailable'}

---
*Vision archived by Oracle Faculty*`;
    }

    /**
     * Update vision disposition
     */
    updateDisposition(visionId, disposition, reason, resultingChanges = []) {
        const vision = this.index.visions.find(v => v.id === visionId);
        if (!vision) {
            console.error(`❌ Vision not found: ${visionId}`);
            return false;
        }

        vision.disposition = disposition;
        vision.dispositionReason = reason;
        vision.dispositionDate = new Date().toISOString();
        vision.resultingChanges = resultingChanges;

        this.saveIndex();
        console.log(`✅ Vision disposition updated: ${visionId} → ${disposition}`);
        return true;
    }

    /**
     * Search visions by criteria
     */
    searchVisions(criteria) {
        return this.index.visions.filter(vision => {
            if (criteria.visionType && vision.visionType !== criteria.visionType) return false;
            if (criteria.trigger && vision.trigger !== criteria.trigger) return false;
            if (criteria.disposition && vision.disposition !== criteria.disposition) return false;
            if (criteria.dateAfter && new Date(vision.timestamp) < new Date(criteria.dateAfter)) return false;
            if (criteria.dateBefore && new Date(vision.timestamp) > new Date(criteria.dateBefore)) return false;
            return true;
        });
    }

    /**
     * Get vision by ID
     */
    getVision(visionId) {
        const indexEntry = this.index.visions.find(v => v.id === visionId);
        if (!indexEntry) return null;

        try {
            const dataPath = path.join(this.archiveDir, indexEntry.dataPath);
            const visionData = JSON.parse(fs.readFileSync(dataPath, 'utf8'));
            return {
                indexEntry,
                ...visionData
            };
        } catch (error) {
            console.error(`❌ Failed to load vision data: ${error.message}`);
            return { indexEntry };
        }
    }
}

module.exports = { VisionArchive };