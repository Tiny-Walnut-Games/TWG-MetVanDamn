#!/usr/bin/env node
/**
 * CID Faculty - Seeded Analysis Templates
 * 
 * Provides the specific analysis patterns and seeded content mentioned
 * in the issue requirements for repository-wide strategic analysis
 */

class SeededAnalysis {
    constructor() {
        this.advisorTemplate = {
            executiveSummary: [
                "Monetization posture: Badge registry + anti‑theft primitives are central; enforce perk immutability and lineage checks in CI for trust.",
                "Learning loop: TLDL-powered rituals are strong; codify 'XP-for-docs' telemetry and surface contributor progress in README badges.",
                "Operational risk: Repo sprawl across genres risks docs drift; establish genre pack templates and a single source of truth.",
                "Pipeline hygiene: Lock down Action permissions, pin actions, and require CODEOWNERS reviews for monetization and badge areas.",
                "Community runway: Shape a 'Schoolhouse' track: quickstart quests, example PRs, and Faculty badges to accelerate first‑time contributors."
            ],
            
            prioritizedActions: [
                {
                    title: "Perk immutability guardrails in CI",
                    effort: "Medium",
                    impact: "High",
                    description: "Add schema checks + signed manifest verification for perks; block changes outside approved migration scripts.",
                    category: "security",
                    priority: 95
                },
                {
                    title: "Genre pack templates",
                    effort: "Medium", 
                    impact: "High",
                    description: "Provide starter docs, quests, and XP tables per genre; DRY via a common template folder and symlink or generator.",
                    category: "documentation",
                    priority: 90
                },
                {
                    title: "Badge registry integrity checks",
                    effort: "Low",
                    impact: "High", 
                    description: "Verify badge IDs, signatures, and issuance logs on PR; publish a public verify endpoint/readme snippet.",
                    category: "security",
                    priority: 88
                },
                {
                    title: "Contributor 'First Quest' path",
                    effort: "Low",
                    impact: "Medium",
                    description: "A single labeled issue + sample PR that walks through docs XP, commit rituals, and Faculty stamps.",
                    category: "community",
                    priority: 75
                },
                {
                    title: "Actions hardening",
                    effort: "Low",
                    impact: "High",
                    description: "`permissions: contents: read`, `pull-requests: write` only where needed; pin SHA for all actions; restrict reusable workflows by `workflow_call`.",
                    category: "security",
                    priority: 85
                },
                {
                    title: "XP telemetry surface",
                    effort: "Medium",
                    impact: "Medium",
                    description: "Emit XP and completeness scores to a JSON artifact; render shields in README and per‑PR comments.",
                    category: "gamification",
                    priority: 70
                },
                {
                    title: "TLDL templates v2", 
                    effort: "Low",
                    impact: "Medium",
                    description: "Add 'Present Wisdom' and 'Future Sight' sections with checklists and CID stamps.",
                    category: "documentation",
                    priority: 65
                }
            ],

            quickWins: [
                "Pin and audit all GitHub Actions SHA.",
                "Add CODEOWNERS for `monetization/`, `badges/`, and `perks/`.",
                "README banner + onboarding quest link above the fold.",
                "Labels: `cid:faculty`, `faculty:proceed`, `quest:first‑contrib`."
            ],

            implementationGuidance: [
                "Perk schema: Define `perks.schema.json` + signed `perks.manifest.json.sig`; validate in PR.",
                "Templates: `/templates/genre/{fantasy,sci‑fi,western,cyberpunk}` with `docs.md`, `quests.md`, `xp.json`.",
                "Actions policy: Require status checks + linear history on `main`; enable branch protection for monetization paths."
            ]
        };

        this.oracleTemplate = {
            scenarios: [
                {
                    name: "Steady ascent",
                    probability: 0.60,
                    timeframe: "90 days",
                    shape: "Organic growth via quests + badges; community PR cadence increases; marketplace inquiries begin.",
                    risks: ["Docs drift across genres", "review bottlenecks for monetization changes"],
                    prerequisites: ["Genre templates shipped", "CODEOWNERS enforced", "XP telemetry visible"],
                    leadingIndicators: ["Weekly stars trend", "first‑PR conversion rate", "time‑to‑first‑review"],
                    successMetrics: ["30% first‑PR conversion", "median review < 24h", "3 genre packs adopted"]
                },
                {
                    name: "Monetization spike & strain",
                    probability: 0.25,
                    timeframe: "90 days",
                    shape: "Badge demand surges; copycats test anti‑theft; support load spikes.",
                    risks: ["Fraud attempts on badge issuance", "CI queue saturation", "governance stress"],
                    prerequisites: ["Public verify endpoint", "rate limits + issuance audit trail", "on‑call reviewer rota"],
                    leadingIndicators: ["Badge verification traffic", "failed signature checks", "PRs touching `monetization/`"],
                    successMetrics: [">99.5% valid verifications", "mean time to triage", "zero unauthorized perk edits on `main`"]
                },
                {
                    name: "Fragmented forks",
                    probability: 0.15,
                    timeframe: "90 days", 
                    shape: "Popular forks diverge; unofficial perks proliferate; attribution blurs.",
                    risks: ["Brand dilution", "support ambiguity", "incompatible perk ecosystems"],
                    prerequisites: ["Clear licensing + trademark guidance", "compatibility kit + 'Official Module' registry"],
                    leadingIndicators: ["Fork/PR ratio", "external module submissions", "inbound questions on compat"],
                    successMetrics: ["1 official 'compat kit'", "registry with ≥10 vetted modules", "≥80% forks tracking upstream monthly"]
                }
            ],

            strategicDecisionSupport: [
                "Pick your lane: Ship official genre packs first, or open the module registry first—not both in the same sprint.",
                "Guard the core: Treat `perks`, `badges`, and `monetization` as sacred; everything else is extensible.",
                "Signal health: Publish a public dashboard of XP, quest completion, and verification stats."
            ]
        };
    }

    /**
     * Apply seeded analysis to advisor report if applicable
     */
    enhanceAdvisorReport(baseReport, context) {
        // Check if this is a repository-wide strategic analysis
        if (this.shouldApplySeededAnalysis(context)) {
            return {
                ...baseReport,
                seeded: true,
                summary: this.advisorTemplate.executiveSummary,
                actionItems: this.advisorTemplate.prioritizedActions,
                quickWins: this.advisorTemplate.quickWins,
                implementationGuidance: this.advisorTemplate.implementationGuidance,
                metadata: {
                    ...baseReport.metadata,
                    seededTemplate: true,
                    templateType: 'strategic-repository-analysis'
                }
            };
        }
        
        return baseReport;
    }

    /**
     * Apply seeded analysis to oracle report if applicable  
     */
    enhanceOracleReport(baseReport, context) {
        // Check if this is a repository-wide strategic analysis
        if (this.shouldApplySeededAnalysis(context)) {
            return {
                ...baseReport,
                seeded: true,
                scenarios: this.oracleTemplate.scenarios,
                strategicDecisionSupport: this.oracleTemplate.strategicDecisionSupport,
                summary: [
                    `Strategic forecasting reveals ${this.oracleTemplate.scenarios.length} possible futures over 90-day horizon`,
                    `Scenario probability range: ${Math.min(...this.oracleTemplate.scenarios.map(s => s.probability)) * 100}% - ${Math.max(...this.oracleTemplate.scenarios.map(s => s.probability)) * 100}%`,
                    `Primary pathways: ${this.oracleTemplate.scenarios.map(s => s.name).join(', ')}`,
                    'Each scenario includes trajectory, prerequisites, risks, and leading indicators',
                    'Strategic decisions require balancing growth opportunities with operational constraints'
                ],
                metadata: {
                    ...baseReport.metadata,
                    seededTemplate: true,
                    templateType: 'strategic-repository-forecast',
                    scenarioCount: this.oracleTemplate.scenarios.length
                }
            };
        }
        
        return baseReport;
    }

    /**
     * Determine if seeded analysis should be applied
     */
    shouldApplySeededAnalysis(context) {
        // Apply seeded analysis for repository-wide strategic analysis
        if (context.scope === 'repository-wide' && context.analysis === 'strategic') {
            return true;
        }

        // Also apply if repository shows signs of being a living dev agent template
        if (this.isLivingDevAgentRepo(context)) {
            return true;
        }

        return false;
    }

    /**
     * Check if repository appears to be a Living Dev Agent template
     */
    isLivingDevAgentRepo(context) {
        const indicators = [
            context.docs?.tldlEntries > 5,
            context.configs?.packageJson && context.topology?.languages?.JavaScript,
            context.topology?.workflows?.some(w => w.includes('faculty') || w.includes('cid')),
            context.docs?.copilotInstructions,
            // Check for specific files that indicate this is a Living Dev Agent repo
            context.topology?.directories?.includes('scripts/cid-faculty'),
            context.docs?.readme?.toLowerCase().includes('living dev agent')
        ];

        // If at least 3 indicators are present, apply seeded analysis
        return indicators.filter(Boolean).length >= 3;
    }

    /**
     * Get seeded content for specific categories
     */
    getSeededContent(type, category = null) {
        if (type === 'advisor') {
            if (category) {
                return this.advisorTemplate[category] || null;
            }
            return this.advisorTemplate;
        }
        
        if (type === 'oracle') {
            if (category) {
                return this.oracleTemplate[category] || null;
            }
            return this.oracleTemplate;
        }
        
        return null;
    }

    /**
     * Generate context-aware enhancements for base analysis
     */
    generateContextualEnhancements(context) {
        const enhancements = {
            advisor: [],
            oracle: []
        };

        // Detect specific patterns in the repository
        if (context.docs?.tldlEntries > 10) {
            enhancements.advisor.push({
                category: 'documentation',
                title: 'TLDL Governance Enhancement',
                description: 'Rich TLDL history detected - implement template versioning and cross-linking automation',
                evidence: `${context.docs.tldlEntries} TLDL entries indicate mature documentation practice`,
                impact: 'medium',
                effort: 'low',
                priority: 70
            });
        }

        if (context.topology?.workflows?.length > 5) {
            enhancements.oracle.push({
                name: 'CI/CD Optimization Path',
                probability: 0.45,
                timeframe: '60 days',
                description: 'Mature pipeline infrastructure enables advanced automation and optimization',
                risks: ['Over-engineering', 'Maintenance overhead'],
                prerequisites: ['Pipeline audit', 'Performance baseline'],
                outcomes: ['Reduced deployment time', 'Enhanced reliability']
            });
        }

        return enhancements;
    }
}

module.exports = { SeededAnalysis };