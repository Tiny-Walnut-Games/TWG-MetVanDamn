#!/usr/bin/env node
/**
 * Chronicle Keeper - Request-for-Format (RFF) Protocol
 * 
 * Handles rejection feedback for content that fails TLDL worthiness or format checks.
 * Provides helpful guidance and examples to contributors for proper resubmission.
 */

const fs = require('fs');
const path = require('path');

class RFFProtocol {
    constructor() {
        this.stats = {
            rffIssued: 0,
            resubmissions: 0,
            successful: 0
        };
    }

    /**
     * Analyze why content was rejected and generate appropriate RFF message
     * @param {Object} content - Original content that was rejected
     * @param {string} rejectionReason - Specific reason for rejection
     * @returns {Object} RFF message with guidance
     */
    generateRFFMessage(content, rejectionReason = 'unknown') {
        this.stats.rffIssued++;
        
        const reasons = this.analyzeRejectionReasons(content);
        
        // If no specific reasons found, use general unworthiness 
        const finalReasons = reasons.length > 0 ? reasons : ['general_unworthiness'];
        
        const examples = this.getFormatExamples(finalReasons);
        const guidance = this.generateGuidance(finalReasons);
        
        return {
            rffId: `RFF-${Date.now()}`,
            timestamp: new Date().toISOString(),
            originalContent: {
                type: content.type,
                id: content.id || content.number,
                title: content.title,
                author: content.author
            },
            rejection: {
                reasons: finalReasons,
                primaryReason: rejectionReason,
                analysis: this.getDetailedAnalysis(content, finalReasons)
            },
            guidance: guidance,
            examples: examples,
            resubmissionPrompt: this.generateResubmissionPrompt(content, finalReasons),
            keeperQuote: this.getKeeperQuote()
        };
    }

    /**
     * Analyze the specific reasons why content was rejected
     * @param {Object} content - Content to analyze
     * @returns {Array} Array of rejection reasons
     */
    analyzeRejectionReasons(content) {
        const reasons = [];
        const text = `${content.title || ''} ${content.body || ''}`;
        
        // Check for missing TLDA format elements
        if (!this.hasTLDAHeader(content)) {
            reasons.push('missing_tlda_header');
        }
        
        if (!this.hasPhaseTag(content)) {
            reasons.push('missing_phase_tag');
        }
        
        if (!this.hasKeeperNote(content)) {
            reasons.push('missing_keeper_note');
        }
        
        // Check content quality issues
        if (text.length < 50) {
            reasons.push('content_too_short');
        }
        
        if (!this.hasLoreKeywords(content)) {
            reasons.push('missing_lore_keywords');
        }
        
        if (this.isBotGenerated(content)) {
            reasons.push('bot_generated');
        }
        
        if (!this.hasActionableContent(content)) {
            reasons.push('not_actionable');
        }
        
        return reasons;
    }

    /**
     * Check if content has proper TLDA header
     * @param {Object} content - Content to check
     * @returns {boolean} True if has TLDA header
     */
    hasTLDAHeader(content) {
        const text = `${content.title || ''} ${content.body || ''}`;
        return /(?:🧠|TLDL:|TLDA:)/i.test(text);
    }

    /**
     * Check if content has phase/quest tags
     * @param {Object} content - Content to check
     * @returns {boolean} True if has phase tags
     */
    hasPhaseTag(content) {
        const text = `${content.title || ''} ${content.body || ''}`;
        return /(?:\*\*Phase\*\*|Phase:|Quest:|Arc:|Discovery:|Implementation:)/i.test(text);
    }

    /**
     * Check if content has Keeper notes or guidance
     * @param {Object} content - Content to check
     * @returns {boolean} True if has Keeper notes
     */
    hasKeeperNote(content) {
        const text = `${content.title || ''} ${content.body || ''}`;
        return /(?:\*\*KeeperNote\*\*|KeeperNote:|📜|Chronicle:|Lore:)/i.test(text);
    }

    /**
     * Check if content has lore-worthy keywords
     * @param {Object} content - Content to check
     * @returns {boolean} True if has lore keywords
     */
    hasLoreKeywords(content) {
        const text = `${content.title || ''} ${content.body || ''}`;
        const loreKeywords = ['🧠', '🪶', '🍑', '📜', 'adventure', 'quest', 'lore', 'discovery', 'implementation'];
        return loreKeywords.some(keyword => text.toLowerCase().includes(keyword.toLowerCase()));
    }

    /**
     * Check if content is bot-generated
     * @param {Object} content - Content to check
     * @returns {boolean} True if bot-generated
     */
    isBotGenerated(content) {
        const botUsers = ['dependabot', 'github-actions', 'renovate'];
        return botUsers.includes(content.author?.toLowerCase());
    }

    /**
     * Check if content has actionable elements
     * @param {Object} content - Content to check
     * @returns {boolean} True if actionable
     */
    hasActionableContent(content) {
        const text = `${content.title || ''} ${content.body || ''}`;
        const actionPatterns = /(?:implement|create|add|fix|update|build|design|test|validate)/i;
        return actionPatterns.test(text);
    }

    /**
     * Get detailed analysis of rejection reasons
     * @param {Object} content - Original content
     * @param {Array} reasons - Rejection reasons
     * @returns {string} Detailed analysis
     */
    getDetailedAnalysis(content, reasons) {
        const analysisMap = {
            'missing_tlda_header': 'Content lacks the sacred brain emoji (🧠) or TLDL: prefix that marks it for chronicle preservation.',
            'missing_phase_tag': 'Content does not specify which development phase or quest arc it belongs to.',
            'missing_keeper_note': 'Content lacks Chronicle Keeper guidance notes (📜) to provide context for future adventurers.',
            'content_too_short': `Content is only ${(`${content.title || ''} ${content.body || ''}`).length} characters, below the minimum threshold for meaningful lore.`,
            'missing_lore_keywords': 'Content lacks keywords that indicate adventure-worthy discoveries or implementations.',
            'bot_generated': 'Content appears to be automatically generated rather than crafted by an adventurer.',
            'not_actionable': 'Content does not contain clear actions, discoveries, or implementations worthy of preservation.',
            'general_unworthiness': 'Content does not meet the overall criteria for lore preservation in the eternal chronicle.'
        };
        
        return reasons.map(reason => analysisMap[reason] || 'Unknown rejection reason').join(' ');
    }

    /**
     * Generate format examples based on rejection reasons
     * @param {Array} reasons - Rejection reasons
     * @returns {Object} Format examples
     */
    getFormatExamples(reasons) {
        const examples = {};
        
        if (reasons.includes('missing_tlda_header')) {
            examples.tlda_header = {
                title: 'Example: Proper TLDA Header',
                content: `🧠 TLDA Epic Quest Implementation

**KeeperNote**: This chronicles the implementation of the epic quest system, a critical milestone in our adventure framework.`
            };
        }
        
        if (reasons.includes('missing_phase_tag')) {
            examples.phase_tag = {
                title: 'Example: Phase Tags',
                content: `**Phase**: Implementation
**Quest Arc**: User Experience Enhancement
**Discovery Type**: Technical Breakthrough`
            };
        }
        
        if (reasons.includes('missing_keeper_note')) {
            examples.keeper_note = {
                title: 'Example: Keeper Notes',
                content: `📜 **KeeperNote**: This discovery fundamentally changes how we approach user authentication, providing both security and ease of use.

**Future Impact**: This implementation will serve as the foundation for all subsequent authentication features.`
            };
        }
        
        if (reasons.includes('content_too_short') || reasons.includes('missing_lore_keywords')) {
            examples.rich_content = {
                title: 'Example: Rich, Lore-Worthy Content',
                content: `🧠 Adventure Discovery: Enhanced Scroll Generation

**Quest Context**: During the implementation of the scroll system, we discovered a critical optimization that improves generation speed by 300%.

**Implementation**: 
- Created new scroll template engine
- Optimized parsing algorithms
- Added adaptive memory management

**Discoveries**:
- Memory usage patterns revealed bottlenecks
- Template caching dramatically improved performance
- User experience metrics showed significant improvement

**Next Adventures**:
- [ ] Implement across all scroll types
- [ ] Create performance monitoring dashboard
- [ ] Document optimization patterns for future quests`
            };
        }
        
        return examples;
    }

    /**
     * Generate guidance for proper resubmission
     * @param {Array} reasons - Rejection reasons
     * @returns {Object} Resubmission guidance
     */
    generateGuidance(reasons) {
        const guidance = {
            quickFixes: [],
            detailedSteps: [],
            bestPractices: []
        };
        
        if (reasons.includes('missing_tlda_header')) {
            guidance.quickFixes.push('Add 🧠 emoji or "TLDL:" prefix to your title');
            guidance.detailedSteps.push('Start your title with the brain emoji (🧠) followed by a descriptive title that indicates this is chronicle-worthy content');
        }
        
        if (reasons.includes('missing_phase_tag')) {
            guidance.quickFixes.push('Add **Phase**: Implementation (or Discovery/Planning) to your content');
            guidance.detailedSteps.push('Include phase tags to help organize content by development lifecycle stage');
        }
        
        if (reasons.includes('missing_keeper_note')) {
            guidance.quickFixes.push('Add **KeeperNote**: explaining why this matters for future adventurers');
            guidance.detailedSteps.push('Include Chronicle Keeper notes that provide context and explain the significance of your content');
        }
        
        if (reasons.includes('content_too_short')) {
            guidance.quickFixes.push('Expand content to at least 50 characters with meaningful detail');
            guidance.detailedSteps.push('Provide sufficient detail about discoveries, implementations, or decisions to be valuable for future reference');
        }
        
        if (reasons.includes('missing_lore_keywords')) {
            guidance.quickFixes.push('Include adventure keywords: discovery, implementation, quest, adventure, or relevant emojis');
            guidance.detailedSteps.push('Use terminology that indicates this content represents meaningful progress in the development adventure');
        }
        
        // General best practices
        guidance.bestPractices = [
            'Use descriptive titles that clearly indicate the adventure or discovery',
            'Include context about why this matters for the project',
            'Describe the implementation or discovery process',
            'Add next steps or follow-up adventures',
            'Use emojis and formatting to make content engaging',
            'Think about future developers who will read this lore'
        ];
        
        return guidance;
    }

    /**
     * Generate resubmission prompt
     * @param {Object} content - Original content
     * @param {Array} reasons - Rejection reasons
     * @returns {string} Resubmission prompt
     */
    generateResubmissionPrompt(content, reasons) {
        const contentType = content.type === 'issue' ? 'issue' : content.type === 'pull_request' ? 'pull request' : 'comment';
        
        return `⚖️ The Chronicler finds this ${contentType} unfit for the scrolls in its current form. 

To have your content preserved in the eternal chronicle, please resubmit with the corrections above. You can:

1. **Edit your existing ${contentType}** to include the missing TLDA format elements
2. **Create a new ${contentType}** with proper format and reference this one
3. **Add a comment** to this ${contentType} with "TLDL: [corrected content]" to trigger re-evaluation

Once corrected, the Chronicler will re-evaluate your submission for inclusion in the sacred scrolls.

*The archives await your properly formatted contribution to the adventure...*`;
    }

    /**
     * Get a thematic quote for the RFF message
     * @returns {string} Keeper quote
     */
    getKeeperQuote() {
        const quotes = [
            "⚖️ *The Chronicler finds this tale unfit for the scrolls in its current form. Present it anew, with the marks and measures of the archive, and it shall be recorded.*",
            "📜 *The Sacred Scrolls require proper incantations. Speak the words of power - the brain emoji, the phase tags, the Keeper's notes - and your tale shall find its place.*",
            "🧙‍♂️ *Even the mightiest adventures begin with proper form. Reshape your words according to the ancient patterns, and the Chronicle shall welcome them.*",
            "⚡ *The Archive Guards have spoken: format grants immortality. Transform your submission according to the sacred template, and achieve eternal preservation.*",
            "🛡️ *The Cheek Preservation Protocol requires proper documentation. Follow the guidance above to ensure your discoveries survive for future adventurers.*"
        ];
        
        return quotes[Math.floor(Math.random() * quotes.length)];
    }

    /**
     * Track resubmission attempt
     * @param {string} rffId - Original RFF ID
     * @param {Object} resubmittedContent - New content
     * @returns {Object} Resubmission result
     */
    trackResubmission(rffId, resubmittedContent) {
        this.stats.resubmissions++;
        
        // Check if resubmission addresses the original issues
        const newReasons = this.analyzeRejectionReasons(resubmittedContent);
        const isImproved = newReasons.length === 0;
        
        if (isImproved) {
            this.stats.successful++;
        }
        
        return {
            rffId: rffId,
            resubmissionTimestamp: new Date().toISOString(),
            improved: isImproved,
            remainingIssues: newReasons,
            readyForChronicle: isImproved
        };
    }

    /**
     * Get RFF Protocol statistics
     * @returns {Object} Statistics
     */
    getStats() {
        return {
            ...this.stats,
            successRate: this.stats.resubmissions > 0 ? (this.stats.successful / this.stats.resubmissions * 100).toFixed(1) + '%' : '0%'
        };
    }
}

// CLI interface for testing
if (require.main === module) {
    const rff = new RFFProtocol();
    
    // Example: Test with a content that would be rejected
    const rejectedContent = {
        type: 'issue',
        id: 123,
        title: 'Small fix',
        body: 'Fixed a bug.',
        author: 'developer'
    };
    
    const rffMessage = rff.generateRFFMessage(rejectedContent, 'content_too_short');
    console.log('📋 RFF Protocol Test Result:');
    console.log(JSON.stringify(rffMessage, null, 2));
    
    console.log('\n📊 RFF Protocol Stats:');
    console.log(rff.getStats());
}

module.exports = RFFProtocol;