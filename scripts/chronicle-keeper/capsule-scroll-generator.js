#!/usr/bin/env node
/**
 * Capsule Scroll Generator
 * 
 * Generates Capsule Scrolls for preserving conversation context when hitting archive walls.
 * Integrates with Chronicle Keeper system for automated generation.
 */

const fs = require('fs');
const path = require('path');
const yaml = require('js-yaml');

class CapsuleScrollGenerator {
    constructor() {
        this.projectRoot = process.env.GITHUB_WORKSPACE || process.cwd();
        this.capsulesDir = path.join(this.projectRoot, 'capsules');
        this.templatesDir = path.join(this.capsulesDir, 'templates');
        this.activeDir = path.join(this.capsulesDir, 'active');
        this.archivedDir = path.join(this.capsulesDir, 'archived');
        
        this.stats = {
            generated: 0,
            archived: 0,
            errors: 0
        };
    }

    /**
     * Generate a new Capsule Scroll from parsed GitHub content
     * @param {Object} parsedContent - Parsed content from Chronicle Keeper
     * @returns {Object} Generation result with filename and content
     */
    async generateCapsuleScroll(parsedContent) {
        try {
            const scrollType = this.determineScrollType(parsedContent);
            const template = await this.loadTemplate(scrollType);
            const populated = this.populateTemplate(template, parsedContent);
            const filename = this.generateFilename(parsedContent);
            
            await this.writeScrollFile(filename, populated);
            
            this.stats.generated++;
            return {
                success: true,
                filename,
                content: populated,
                scrollType,
                stats: this.stats
            };
            
        } catch (error) {
            this.stats.errors++;
            return {
                success: false,
                error: error.message,
                stats: this.stats
            };
        }
    }

    /**
     * Determine what type of scroll to generate based on content
     * @param {Object} parsedContent - Parsed GitHub content
     * @returns {string} Template type to use
     */
    determineScrollType(parsedContent) {
        // Look for specific decision-making context (not just words)
        const decisionKeywords = ['choose between', 'option a', 'option b', 'alternatives', 'pros and cons'];
        const contentText = (parsedContent.body || '').toLowerCase();
        
        const hasStrongDecisionContext = decisionKeywords.some(keyword => 
            contentText.includes(keyword)
        );
        
        if (hasStrongDecisionContext) {
            return 'decision-capsule';
        }
        
        // For archive wall and continuity contexts, use arc template
        if (contentText.includes('archive wall') || contentText.includes('continuity') || 
            contentText.includes('conversation') || parsedContent.title.includes('🧠📜')) {
            return 'arc-capsule';
        }
        
        // Default to arc capsule for conversation preservation
        return 'arc-capsule';
    }

    /**
     * Load template file
     * @param {string} templateType - Type of template (arc-capsule or decision-capsule)
     * @returns {string} Template content
     */
    async loadTemplate(templateType) {
        const templatePath = path.join(this.templatesDir, `${templateType}.md`);
        
        if (!fs.existsSync(templatePath)) {
            throw new Error(`Template not found: ${templatePath}`);
        }
        
        return fs.readFileSync(templatePath, 'utf8');
    }

    /**
     * Populate template with actual content
     * @param {string} template - Template string with placeholders
     * @param {Object} parsedContent - Data to populate template with
     * @returns {string} Populated template
     */
    populateTemplate(template, parsedContent) {
        const now = new Date().toISOString();
        const dateStr = now.split('T')[0]; // YYYY-MM-DD format
        
        // Create replacement map
        const replacements = {
            '{ARC_NAME}': parsedContent.title || 'Untitled Conversation Arc',
            '{DECISION_NAME}': parsedContent.title || 'Untitled Decision',
            '{START_DATE}': parsedContent.created_at ? parsedContent.created_at.split('T')[0] : dateStr,
            '{END_DATE}': dateStr,
            '{DECISION_DATE}': parsedContent.created_at ? parsedContent.created_at.split('T')[0] : dateStr,
            '{CONVERSATION_MODE}': this.inferConversationMode(parsedContent),
            '{ISSUE_OR_PR_CONTEXT}': parsedContent.url || 'Context not available',
            '{PARTICIPANTS}': parsedContent.author || 'Unknown',
            '{CREATION_DATE}': now,
            '{UPDATE_DATE}': now,
            '{STATUS}': 'Active',
            '{ACTIVE_OR_ARCHIVED}': 'Active'
        };

        // Extract features, decisions, artifacts from content
        this.extractContentElements(parsedContent, replacements);
        
        // Replace all placeholders, including ones that weren't explicitly set
        let populated = template;
        
        // First pass: replace explicit values
        for (const [placeholder, value] of Object.entries(replacements)) {
            populated = populated.replace(new RegExp(placeholder.replace(/[{}]/g, '\\$&'), 'g'), value);
        }
        
        // Second pass: replace any remaining placeholders with sensible defaults
        const defaultReplacements = {
            '{DESCRIPTION_OF_THE_PROBLEM_OR_CHOICE_THAT_NEEDED_TO_BE_MADE}': 'Archive wall context preservation challenge - need to maintain conversation continuity when hitting context limits.',
            '{OPTION_A_NAME}': 'Manual Context Summarization',
            '{OPTION_B_NAME}': 'Capsule Scrolls System',
            '{OPTION_C_NAME}': 'Enhanced TLDL Integration',
            '{PROS_LIST}': 'Preserves context, enables continuation, saves developer time',
            '{CONS_LIST}': 'Requires initial setup, needs maintenance',
            '{IMPACT_ASSESSMENT}': 'High positive impact on developer experience',
            '{CHOSEN_OPTION}': 'Capsule Scrolls System',
            '{EXPLANATION_OF_WHY_THIS_OPTION_WAS_CHOSEN}': 'Provides automated, structured approach to context preservation that integrates with existing Chronicle Keeper system.',
            '{FACTOR_1}': 'Addresses immediate archive wall problem',
            '{FACTOR_2}': 'Builds on existing infrastructure',
            '{FACTOR_3}': 'Aligns with "Save the Butts" philosophy',
            '{IMPLEMENTATION_TIMELINE}': 'Current implementation phase',
            '{CHANGES_REQUIRED}': 'Add capsules directory, templates, generator script, Chronicle Keeper integration',
            '{HOW_TO_MEASURE_SUCCESS}': 'Successful context preservation across conversation boundaries',
            '{WHAT_TO_DO_IF_IT_GOES_WRONG}': 'Fall back to manual TLDL creation and context summarization',
            '{ACTION_ITEM_1}': 'Complete Capsule Scroll generator implementation',
            '{ACTION_ITEM_2}': 'Integrate with Chronicle Keeper workflow',
            '{ACTION_ITEM_3}': 'Test with real conversation scenarios',
            '{PARTICIPANT_1}': `${parsedContent.author || 'contributor'} — Primary implementer`,
            '{PARTICIPANT_2}': 'Chronicle Keeper — Automation system',
            '{RUNNING_JOKE_OR_MEME}': 'Save the Butts continuity ritual',
            '{PROJECT_SPECIFIC_TERMINOLOGY}': 'Archive wall, molting ritual for code, Chronicle Keeper',
            '{NEXT_REVIEW_DATE}': new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0] // 1 week from now
        };
        
        for (const [placeholder, defaultValue] of Object.entries(defaultReplacements)) {
            populated = populated.replace(new RegExp(placeholder.replace(/[{}]/g, '\\$&'), 'g'), defaultValue);
        }
        
        return populated;
    }

    /**
     * Extract content elements from parsed GitHub content
     * @param {Object} parsedContent - Parsed GitHub content
     * @param {Object} replacements - Replacement map to populate
     */
    extractContentElements(parsedContent, replacements) {
        const body = parsedContent.body || '';
        
        // Try to extract key elements from the content
        const lines = body.split('\n');
        
        // Look for features/decisions/artifacts in bullet points or numbered lists
        const features = [];
        const decisions = [];
        const artifacts = [];
        const glyphs = [];
        const threads = [];
        
        for (const line of lines) {
            const trimmed = line.trim();
            
            // Extract bullet points and numbered items
            if (trimmed.match(/^[-*+]\s+/) || trimmed.match(/^\d+\.\s+/)) {
                const content = trimmed.replace(/^[-*+\d.]\s*/, '');
                
                if (content.toLowerCase().includes('feature') || content.toLowerCase().includes('implement')) {
                    features.push(content);
                } else if (content.toLowerCase().includes('decision') || content.toLowerCase().includes('choose')) {
                    decisions.push(content);
                } else if (content.includes('http') || content.includes('github.com')) {
                    artifacts.push(content);
                } else if (content.includes('🧠') || content.includes('📜') || content.includes('🍑')) {
                    glyphs.push(content);
                } else if (content.toLowerCase().includes('todo') || content.toLowerCase().includes('unresolved')) {
                    threads.push(content);
                }
            }
        }
        
        // Populate replacements with extracted content or defaults
        replacements['{FEATURE_1}'] = features[0] || 'Archive Wall Context Preservation';
        replacements['{FEATURE_2}'] = features[1] || 'Capsule Scroll Generation';
        replacements['{FEATURE_3}'] = features[2] || 'Chronicle Keeper Integration';
        
        replacements['{BRIEF_DESCRIPTION}'] = 'Implementation to preserve conversation context when hitting archive walls';
        
        replacements['{DECISION_1}'] = decisions[0] || 'Implement Capsule Scroll system for context preservation';
        replacements['{DECISION_2}'] = decisions[1] || 'Use existing daily ledger format as template base';
        replacements['{DECISION_3}'] = decisions[2] || 'Integrate with Chronicle Keeper for automation';
        
        replacements['{ARTIFACT_1}'] = artifacts[0] || `[Issue #${parsedContent.number || 'unknown'}](${parsedContent.url || '#'})`;
        replacements['{ARTIFACT_2}'] = artifacts[1] || '[Capsule Scrolls README](../README.md)';
        replacements['{ARTIFACT_3}'] = artifacts[2] || '[Chronicle Keeper Integration](../scripts/chronicle-keeper/)';
        
        replacements['{GLYPH_1}'] = glyphs[0] || '🧠📜 — Archive Wall Continuity Ritual';
        replacements['{GLYPH_2}'] = glyphs[1] || '🍑 — Save the Butts clause'; 
        replacements['{GLYPH_3}'] = glyphs[2] || '🧙‍♂️ — Chronicle Keeper automation';
        
        replacements['{CONTEXT}'] = 'Preserves development wisdom and conversation continuity';
        
        replacements['{THREAD_1}'] = threads[0] || 'Test automated generation workflow';
        replacements['{FEATURE_1}'] = features[0] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{FEATURE_1}'];
        replacements['{FEATURE_2}'] = features[1] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{FEATURE_2}'];
        replacements['{FEATURE_3}'] = features[2] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{FEATURE_3}'];
        
        replacements['{BRIEF_DESCRIPTION}'] = CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{BRIEF_DESCRIPTION}'];
        
        replacements['{DECISION_1}'] = decisions[0] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{DECISION_1}'];
        replacements['{DECISION_2}'] = decisions[1] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{DECISION_2}'];
        replacements['{DECISION_3}'] = decisions[2] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{DECISION_3}'];
        
        replacements['{ARTIFACT_1}'] = artifacts[0] || `[Issue #${parsedContent.number || 'unknown'}](${parsedContent.url || '#'})`;
        replacements['{ARTIFACT_2}'] = artifacts[1] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{ARTIFACT_2}'];
        replacements['{ARTIFACT_3}'] = artifacts[2] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{ARTIFACT_3}'];
        
        replacements['{GLYPH_1}'] = glyphs[0] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{GLYPH_1}'];
        replacements['{GLYPH_2}'] = glyphs[1] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{GLYPH_2}']; 
        replacements['{GLYPH_3}'] = glyphs[2] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{GLYPH_3}'];
        
        replacements['{CONTEXT}'] = CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{CONTEXT}'];
        
        replacements['{THREAD_1}'] = threads[0] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{THREAD_1}'];
        replacements['{THREAD_2}'] = threads[1] || CapsuleScrollGenerator.DEFAULT_REPLACEMENTS['{THREAD_2}'];
        replacements['{THREAD_3}'] = threads[2] || 'Create proof-of-concept scroll for this implementation';
        
        // Generate re-entry spell
        const reentrySpell = this.generateReentrySpell(parsedContent);
        replacements['{THREE_SENTENCE_SUMMARY_OF_THE_CONVERSATION_ARC_THAT_ENABLES_QUICK_CONTEXT_RESTORATION}'] = reentrySpell;
        
        // Add reason/status defaults
        replacements['{REASON}'] = 'implementation in progress';
        replacements['{STATUS}'] = 'pass';
        
        // Add links
        replacements['{LINK_1}'] = parsedContent.url || '#';
        replacements['{LINK_2}'] = '../README.md';
        replacements['{LINK_3}'] = '../scripts/chronicle-keeper/';
        
        replacements['{TLDL_ENTRY_1}'] = '[Chronicle Keeper Implementation](../../TLDL/entries/TLDL-2025-08-07-ChronicleKeeperImplementation.md)';
        replacements['{TLDL_ENTRY_2}'] = '[Daily Ledger Format](../../docs/daily-ledger/2025-08-18.md)';
        replacements['{TLDL_LINK_1}'] = '../../TLDL/entries/TLDL-2025-08-07-ChronicleKeeperImplementation.md';
        replacements['{TLDL_LINK_2}'] = '../../docs/daily-ledger/2025-08-18.md';
        
        replacements['{ISSUE_1}'] = `Issue #${parsedContent.number || 'unknown'}`;
        replacements['{PR_1}'] = 'PR for Capsule Scrolls Implementation';
        replacements['{ISSUE_LINK_1}'] = parsedContent.url || '#';
        replacements['{PR_LINK_1}'] = '#';
    }

    /**
     * Generate a re-entry spell (3-sentence summary)
     * @param {Object} parsedContent - Parsed GitHub content  
     * @returns {string} Three-sentence summary
     */
    generateReentrySpell(parsedContent) {
        const title = parsedContent.title || 'Archive Wall Conversation';
        const author = parsedContent.author || 'the team';
        const context = parsedContent.body ? 
            parsedContent.body.substring(0, 100).replace(/\n/g, ' ') + '...' : 
            'context preservation implementation';
        
        return `${author} initiated work on "${title}" to address archive wall limitations in long conversations. ` +
               `The implementation focuses on ${context} using the Chronicle Keeper system. ` +
               `This Capsule Scroll preserves the conversation context to enable smooth re-entry when context limits are reached.`;
    }

    /**
     * Infer conversation mode from content
     * @param {Object} parsedContent - Parsed GitHub content
     * @returns {string} Conversation mode
     */
    inferConversationMode(parsedContent) {
        const body = (parsedContent.body || '').toLowerCase();
        
        if (body.includes('keeper') || body.includes('chronicle')) {
            return 'Keeper-plus mode';
        } else if (body.includes('debug') || body.includes('troubleshoot')) {
            return 'Debug & investigation';
        } else if (body.includes('feature') || body.includes('implement')) {
            return 'Feature development';
        } else {
            return 'General development discussion';
        }
    }

    /**
     * Generate filename for the scroll
     * @param {Object} parsedContent - Parsed GitHub content
     * @returns {string} Filename
     */
    generateFilename(parsedContent) {
        const now = new Date();
        const dateStr = now.toISOString().split('T')[0]; // YYYY-MM-DD
        
        let title = parsedContent.title || 'untitled-scroll';
        title = title.replace(/[🧠📜🍑🧙‍♂️]/g, ''); // Remove emojis
        title = title.replace(/[^a-zA-Z0-9\s-]/g, ''); // Remove special chars
        title = title.replace(/\s+/g, '-'); // Replace spaces with hyphens
        title = title.toLowerCase();
        title = title.substring(0, 50); // Limit length
        
        return `${dateStr}-${title}.md`;
    }

    /**
     * Write scroll content to file
     * @param {string} filename - Name of file to create
     * @param {string} content - Content to write
     */
    async writeScrollFile(filename, content) {
        const filepath = path.join(this.activeDir, filename);
        
        // Ensure directory exists
        if (!fs.existsSync(this.activeDir)) {
            fs.mkdirSync(this.activeDir, { recursive: true });
        }
        
        fs.writeFileSync(filepath, content, 'utf8');
    }

    /**
     * Archive an active scroll (move from active/ to archived/)
     * @param {string} filename - Name of file to archive
     * @returns {Object} Result of archiving operation
     */
    async archiveScroll(filename) {
        try {
            const activePath = path.join(this.activeDir, filename);
            const archivedPath = path.join(this.archivedDir, filename);
            
            if (!fs.existsSync(activePath)) {
                throw new Error(`Active scroll not found: ${filename}`);
            }
            
            // Ensure archived directory exists
            if (!fs.existsSync(this.archivedDir)) {
                fs.mkdirSync(this.archivedDir, { recursive: true });
            }
            
            // Move file
            fs.renameSync(activePath, archivedPath);
            this.stats.archived++;
            
            return {
                success: true,
                message: `Archived scroll: ${filename}`,
                stats: this.stats
            };
            
        } catch (error) {
            this.stats.errors++;
            return {
                success: false,
                error: error.message,
                stats: this.stats
            };
        }
    }

    /**
     * List all scrolls (active and archived)
     * @returns {Object} Lists of scrolls by status
     */
    listScrolls() {
        const activeScrolls = fs.existsSync(this.activeDir) ? 
            fs.readdirSync(this.activeDir).filter(f => f.endsWith('.md')) : [];
        
        const archivedScrolls = fs.existsSync(this.archivedDir) ? 
            fs.readdirSync(this.archivedDir).filter(f => f.endsWith('.md')) : [];
        
        return {
            active: activeScrolls,
            archived: archivedScrolls,
            total: activeScrolls.length + archivedScrolls.length
        };
    }

    /**
     * Get current generation statistics
     * @returns {Object} Current stats
     */
    getStats() {
        return { ...this.stats };
    }
}

// CLI interface for direct usage
if (require.main === module) {
    const generator = new CapsuleScrollGenerator();
    
    // Example usage with mock GitHub issue data
    const mockIssue = {
        number: 57,
        title: "🧠📜 Archive Wall Capsule Scrolls — \"Save the Butts\" Continuity Ritual",
        body: `Ha — double‑joke achievement unlocked. 🏆 The "make this an issue" line *is* already a meta‑gag when you're literally creating… an issue...

**Describe the solution you'd like**  
Introduce **Capsule Scrolls** — a compact, ritualized markdown artifact capturing:  
1. Arc Name  
2. Timeframe  
3. Core Decisions  
4. Key Artifacts & Commits  
5. Glyphs & Running Jokes  
6. Unresolved Threads  
7. *Re‑entry Spell* (3‑sentence context snapshot for scene‑reset)  

Stored alongside the relevant commits or in \`/capsules/\`, linked into the TLDL.`,
        user: { login: "jmeyer1980" },
        created_at: new Date().toISOString(),
        html_url: "https://github.com/jmeyer1980/living-dev-agent/issues/57"
    };
    
    // Parse the mock issue (simulate Chronicle Keeper parsing)
    const parsedContent = {
        type: 'issue',
        number: mockIssue.number,
        title: mockIssue.title,
        body: mockIssue.body,
        author: mockIssue.user.login,
        created_at: mockIssue.created_at,
        url: mockIssue.html_url,
        lore_worthy: true,
        category: 'capsule_scroll'
    };
    
    console.log("📜 Capsule Scroll Generator - Chronicle Keeper Integration");
    console.log("=========================================================");
    
    generator.generateCapsuleScroll(parsedContent).then(result => {
        console.log("🧠📜 Generation Result:");
        console.log(JSON.stringify(result, null, 2));
        
        console.log("\n📊 Current Stats:");
        console.log(generator.getStats());
        
        console.log("\n📋 Scroll Inventory:");
        console.log(generator.listScrolls());
    }).catch(error => {
        console.error("❌ Generation failed:", error);
    });
}

module.exports = CapsuleScrollGenerator;