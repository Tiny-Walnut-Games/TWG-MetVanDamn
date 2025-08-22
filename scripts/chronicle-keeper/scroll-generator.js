#!/usr/bin/env node
/**
 * Chronicle Keeper - Scroll Generator
 * 
 * Generates markdown TLDL entries using parsed data and lore templates.
 * Integrates with the existing ScrollQuoteEngine for ambient inspiration.
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');
const yaml = require('js-yaml');

class ScrollGenerator {
    constructor(configPath = null) {
        this.config = this.loadConfig(configPath);
        this.projectRoot = this.findProjectRoot();
        this.stats = {
            generated: 0,
            errors: 0
        };
    }

    loadConfig(configPath) {
        const defaultConfigPath = path.join(__dirname, 'scribe-config.yml');
        const finalPath = configPath || defaultConfigPath;
        
        try {
            const configContent = fs.readFileSync(finalPath, 'utf8');
            return yaml.load(configContent);
        } catch (error) {
            console.error('🚨 Failed to load Chronicle Keeper config:', error.message);
            return { scribe: { name: "Chronicle Keeper" } };
        }
    }

    findProjectRoot() {
        let currentDir = __dirname;
        while (currentDir !== path.dirname(currentDir)) {
            if (fs.existsSync(path.join(currentDir, 'src', 'ScrollQuoteEngine'))) {
                return currentDir;
            }
            currentDir = path.dirname(currentDir);
        }
        return path.dirname(path.dirname(__dirname)); // fallback
    }

    /**
     * Get a contextual quote from the ScrollQuoteEngine
     * @param {string} context - Context for quote selection
     * @returns {string} Formatted quote or fallback
     */
    getContextualQuote(context = 'general') {
        try {
            const quoteEnginePath = path.join(this.projectRoot, 'src', 'ScrollQuoteEngine', 'quote_engine.py');
            
            if (!fs.existsSync(quoteEnginePath)) {
                return this.getFallbackQuote();
            }

            // Map category to quote engine context
            const contextMapping = this.config.integration?.quote_engine?.context_mapping || {};
            const mappedContext = contextMapping[context] || context;

            const command = `cd "${this.projectRoot}" && python3 "${quoteEnginePath}" --context "${mappedContext}" --format markdown 2>/dev/null`;
            const quote = execSync(command, { encoding: 'utf8', timeout: 5000 }).trim();
            
            return quote || this.getFallbackQuote();
        } catch (error) {
            console.warn('⚠️ ScrollQuoteEngine unavailable, using fallback quote');
            return this.getFallbackQuote();
        }
    }

    getFallbackQuote() {
        const fallbackQuotes = [
            '> *"When the quote engine fails, improvisation becomes the highest art."* — **Emergency Protocols, Vol. 404**',
            '> *"Every chronicle entry is a scroll worthy of preservation."* — **Chronicle Keeper\'s Codex, Vol. I**',
            '> *"The adventure continues, even when the magic falters."* — **Backup Wisdom, Vol. Zero**'
        ];
        return fallbackQuotes[Math.floor(Math.random() * fallbackQuotes.length)];
    }

    /**
     * Generate TLDL entry from parsed content
     * @param {Object} parsedContent - Parsed content from ScribeParser
     * @returns {string} Generated markdown content
     */
    generateScrollEntry(parsedContent) {
        this.stats.generated++;
        
        const date = new Date(parsedContent.created_at || new Date()).toISOString().split('T')[0];
        const title = this.sanitizeTitle(parsedContent.title || `${parsedContent.type}-${parsedContent.id}`);
        const entryId = `TLDL-${date}-${title}`;
        
        // Get contextual quote
        const quote = this.getContextualQuote(parsedContent.category);
        
        // Generate the scroll content
        const scrollContent = this.generateScrollContent(parsedContent, entryId, quote);
        
        return {
            filename: `${date}-${title}.md`,
            content: scrollContent,
            metadata: {
                entryId,
                date,
                title,
                category: parsedContent.category,
                author: parsedContent.author,
                priority: parsedContent.priority || 'medium'
            }
        };
    }

    /**
     * Generate the main scroll content
     * @param {Object} parsedContent - Parsed content
     * @param {string} entryId - Entry identifier
     * @param {string} quote - Contextual quote
     * @returns {string} Markdown content
     */
    generateScrollContent(parsedContent, entryId, quote) {
        const date = new Date(parsedContent.created_at || new Date()).toISOString().split('T')[0];
        const author = parsedContent.author || 'unknown-adventurer';
        
        let content = `# ${entryId}\n\n`;
        content += `**Entry ID:** ${entryId}  \n`;
        content += `**Author:** @${author}  \n`;
        content += `**Context:** ${this.generateContextDescription(parsedContent)}  \n`;
        content += `**Summary:** ${this.generateSummary(parsedContent)}  \n\n`;
        content += `---\n\n`;
        content += `${quote}\n\n`;
        content += `---\n\n`;
        
        // Generate discoveries section
        content += this.generateDiscoveries(parsedContent);
        
        // Generate actions taken section  
        content += this.generateActionsTaken(parsedContent);
        
        // Generate technical details if applicable
        content += this.generateTechnicalDetails(parsedContent);
        
        // Generate lessons learned
        content += this.generateLessonsLearned(parsedContent);
        
        // Generate next steps
        content += this.generateNextSteps(parsedContent);
        
        // Generate references
        content += this.generateReferences(parsedContent);
        
        // Generate metadata footer
        content += this.generateMetadataFooter(parsedContent);
        
        return content;
    }

    generateContextDescription(parsedContent) {
        switch (parsedContent.type) {
            case 'issue':
                // Handle synthetic IDs for manual dispatch and fallback
                if (parsedContent.id && typeof parsedContent.id === 'string' && 
                    (parsedContent.id.startsWith('MD') || parsedContent.id.startsWith('FB'))) {
                    const contextType = parsedContent.id.startsWith('MD') ? 'Manual Dispatch' : 'Fallback';
                    return `${contextType} Entry #${parsedContent.id}`;
                }
                return `Issue #${parsedContent.id} - ${parsedContent.category} investigation`;
            case 'pull_request':
                return `PR #${parsedContent.id} - ${parsedContent.merged ? 'Merged' : 'Pending'} changes`;
            case 'comment':
                return `Comment discussion on Issue #${parsedContent.issue_number}`;
            case 'workflow_run':
                return `CI Workflow - ${parsedContent.name} (${parsedContent.conclusion})`;
            default:
                return `${parsedContent.type} - ${parsedContent.category}`;
        }
    }

    generateSummary(parsedContent) {
        const loreContent = parsedContent.lore_content || [];
        if (loreContent.length > 0) {
            return loreContent[0].substring(0, 100) + (loreContent[0].length > 100 ? '...' : '');
        }
        
        switch (parsedContent.type) {
            case 'issue':
                return `Feature request or issue discussion: ${parsedContent.title}`;
            case 'pull_request':
                return `Code changes ${parsedContent.merged ? 'merged' : 'proposed'}: ${parsedContent.title}`;
            case 'comment':
                return `Community discussion and insights shared`;
            case 'workflow_run':
                return `CI pipeline ${parsedContent.conclusion} - ${parsedContent.name}`;
            default:
                return `Development activity recorded: ${parsedContent.title || 'untitled'}`;
        }
    }

    generateDiscoveries(parsedContent) {
        let content = `## Discoveries\n\n`;
        
        if (parsedContent.lore_content && parsedContent.lore_content.length > 0) {
            content += `### Lore-Worthy Insights\n`;
            parsedContent.lore_content.forEach((insight, index) => {
                content += `- **Key Finding ${index + 1}**: ${insight}\n`;
                content += `- **Impact**: Preserves valuable development wisdom for future adventurers\n`;
                content += `- **Evidence**: [${parsedContent.type} #${parsedContent.id}](${parsedContent.url})\n\n`;
            });
        }

        if (parsedContent.tldl_tags && parsedContent.tldl_tags.length > 0) {
            content += `### TLDL Tags Discovered\n`;
            parsedContent.tldl_tags.forEach((tag, index) => {
                content += `- **${tag.language} Tag**: \`${tag.tag_pattern}\`\n`;
                content += `- **Content**: ${tag.content}\n`;
                content += `- **Purpose**: Developer-embedded lore marker for future reference\n\n`;
            });
        }

        // Add type-specific discoveries
        switch (parsedContent.type) {
            case 'pull_request':
                if (parsedContent.merged) {
                    content += `### Code Integration Success\n`;
                    content += `- **Key Finding**: Successfully merged ${parsedContent.commits} commits\n`;
                    content += `- **Impact**: ${parsedContent.additions} additions, ${parsedContent.deletions} deletions across ${parsedContent.changed_files} files\n`;
                    content += `- **Evidence**: Branch ${parsedContent.head_branch} → ${parsedContent.base_branch}\n\n`;
                }
                break;
            case 'workflow_run':
                content += `### CI Pipeline Insights\n`;
                content += `- **Key Finding**: Workflow "${parsedContent.name}" reached ${parsedContent.conclusion}\n`;
                content += `- **Impact**: ${parsedContent.conclusion === 'success' ? 'Quality gates passed' : 'Issues detected requiring attention'}\n`;
                content += `- **Evidence**: [Workflow Run #${parsedContent.run_number}](${parsedContent.url})\n\n`;
                break;
        }

        return content;
    }

    generateActionsTaken(parsedContent) {
        let content = `## Actions Taken\n\n`;
        
        switch (parsedContent.type) {
            case 'issue':
                content += `1. **Issue Creation and Analysis**\n`;
                content += `   - **What**: ${parsedContent.state === 'open' ? 'Opened' : 'Processed'} issue "${parsedContent.title}"\n`;
                content += `   - **Why**: ${parsedContent.category} requires community attention and solution\n`;
                content += `   - **How**: GitHub issue tracking with labels: ${(parsedContent.labels || []).join(', ')}\n`;
                content += `   - **Result**: Issue documented and available for community response\n\n`;
                break;
                
            case 'pull_request':
                content += `1. **Code Changes Implementation**\n`;
                content += `   - **What**: ${parsedContent.merged ? 'Merged' : 'Proposed'} changes in PR #${parsedContent.id}\n`;
                content += `   - **Why**: Address requirements outlined in "${parsedContent.title}"\n`;
                content += `   - **How**: Branch-based development with ${parsedContent.commits} commits\n`;
                content += `   - **Result**: ${parsedContent.merged ? 'Code integrated successfully' : 'Awaiting review and integration'}\n\n`;
                break;
                
            case 'comment':
                content += `1. **Community Engagement**\n`;
                content += `   - **What**: Participated in discussion thread\n`;
                content += `   - **Why**: Share insights and contribute to collaborative problem-solving\n`;
                content += `   - **How**: Posted detailed response with context and suggestions\n`;
                content += `   - **Result**: Added value to community knowledge base\n\n`;
                break;
                
            case 'workflow_run':
                content += `1. **Automated Quality Assurance**\n`;
                content += `   - **What**: Executed CI workflow "${parsedContent.name}"\n`;
                content += `   - **Why**: Ensure code quality and prevent regressions\n`;
                content += `   - **How**: Automated testing and validation pipeline\n`;
                content += `   - **Result**: ${parsedContent.conclusion === 'success' ? 'All checks passed' : 'Issues identified for resolution'}\n\n`;
                break;
        }

        content += `2. **Chronicle Keeper Documentation**\n`;
        content += `   - **What**: Automated TLDL entry generation\n`;
        content += `   - **Why**: Preserve development lore and maintain historical context\n`;
        content += `   - **How**: Scribe system parsing and scroll generation\n`;
        content += `   - **Result**: Knowledge preserved for future adventurers\n\n`;

        return content;
    }

    generateTechnicalDetails(parsedContent) {
        let content = `## Technical Details\n\n`;

        if (parsedContent.type === 'pull_request' && parsedContent.merged) {
            content += `### Code Changes\n`;
            content += '```diff\n';
            content += `# PR #${parsedContent.id}: ${parsedContent.title}\n`;
            content += `# ${parsedContent.additions} additions, ${parsedContent.deletions} deletions\n`;
            content += `# ${parsedContent.changed_files} files changed\n`;
            content += '# Merged from ' + parsedContent.head_branch + ' to ' + parsedContent.base_branch + '\n';
            content += '```\n\n';
        }

        if (parsedContent.tldl_tags && parsedContent.tldl_tags.length > 0) {
            content += `### TLDL Tags Configuration\n`;
            content += '```yaml\n';
            parsedContent.tldl_tags.forEach(tag => {
                content += `${tag.language}:\n`;
                content += `  pattern: "${tag.tag_pattern}"\n`;
                content += `  content: "${tag.content}"\n`;
            });
            content += '```\n\n';
        }

        content += `### Chronicle Keeper Metadata\n`;
        content += '```json\n';
        content += JSON.stringify({
            type: parsedContent.type,
            category: parsedContent.category,
            priority: parsedContent.priority,
            lore_worthy: parsedContent.lore_worthy,
            parsing_timestamp: new Date().toISOString()
        }, null, 2);
        content += '\n```\n\n';

        return content;
    }

    generateLessonsLearned(parsedContent) {
        let content = `## Lessons Learned\n\n`;
        
        content += `### What Worked Well\n`;
        content += `- Chronicle Keeper successfully identified lore-worthy content\n`;
        content += `- Automated parsing and scroll generation maintained efficiency\n`;
        content += `- Integration with ScrollQuoteEngine provided contextual inspiration\n\n`;
        
        content += `### What Could Be Improved\n`;
        content += `- ${parsedContent.type} parsing could benefit from enhanced context analysis\n`;
        content += `- Community engagement patterns suggest opportunities for deeper lore extraction\n`;
        content += `- Cross-reference linkage with existing TLDL entries for pattern recognition\n\n`;
        
        content += `### Knowledge Gaps Identified\n`;
        content += `- Long-term impact assessment of ${parsedContent.category} activities\n`;
        content += `- Integration opportunities with additional development tools\n`;
        content += `- Community feedback mechanisms for Chronicle Keeper improvements\n\n`;

        return content;
    }

    generateNextSteps(parsedContent) {
        let content = `## Next Steps\n\n`;
        
        content += `### Immediate Actions (High Priority)\n`;
        content += `- [ ] Validate TLDL entry accuracy and completeness\n`;
        content += `- [ ] Cross-reference with related development activities\n`;
        content += `- [ ] Update TLDL index with new entry metadata\n\n`;
        
        content += `### Medium-term Actions (Medium Priority)\n`;
        if (parsedContent.type === 'issue' && parsedContent.state === 'open') {
            content += `- [ ] Monitor issue #${parsedContent.id} for resolution and updates\n`;
        }
        if (parsedContent.type === 'pull_request' && !parsedContent.merged) {
            content += `- [ ] Track PR #${parsedContent.id} through review and merge process\n`;
        }
        content += `- [ ] Analyze patterns across similar ${parsedContent.category} entries\n`;
        content += `- [ ] Consider automation enhancements based on parsing results\n\n`;
        
        content += `### Long-term Considerations (Low Priority)\n`;
        content += `- [ ] Develop predictive analytics for ${parsedContent.category} trends\n`;
        content += `- [ ] Create specialized parsing rules for recurring patterns\n`;
        content += `- [ ] Establish community feedback loop for Chronicle Keeper improvements\n\n`;

        return content;
    }

    generateReferences(parsedContent) {
        let content = `## References\n\n`;
        
        content += `### Internal Links\n`;
        content += `- Original ${parsedContent.type}: [#${parsedContent.id}](${parsedContent.url})\n`;
        content += `- TLDL Index: [TLDL/index.md](../index.md)\n`;
        content += `- Chronicle Keeper Config: [scribe-config.yml](../../scripts/chronicle-keeper/scribe-config.yml)\n\n`;
        
        content += `### External Resources\n`;
        content += `- GitHub ${parsedContent.type} documentation\n`;
        content += `- Living Dev Agent methodology\n`;
        content += `- ScrollQuoteEngine wisdom database\n\n`;

        return content;
    }

    generateMetadataFooter(parsedContent) {
        const now = new Date().toISOString();
        
        let content = `---\n\n`;
        content += `## TLDL Metadata\n\n`;
        content += `**Tags**: #${parsedContent.category} #${parsedContent.type} #chronicle-keeper  \n`;
        content += `**Complexity**: Medium  \n`;
        content += `**Impact**: ${parsedContent.priority === 'high' ? 'High' : parsedContent.priority === 'low' ? 'Low' : 'Medium'}  \n`;
        content += `**Team Members**: @${parsedContent.author}  \n`;
        content += `**Duration**: Automated processing  \n`;
        content += `**Related Epic**: Chronicle Keeper Implementation  \n\n`;
        content += `---\n\n`;
        content += `**Created**: ${now}  \n`;
        content += `**Last Updated**: ${now}  \n`;
        content += `**Status**: Complete  \n\n`;
        content += `*Generated by Chronicle Keeper - Preserving the lore, one scroll at a time.*\n`;

        return content;
    }

    sanitizeTitle(title) {
        return title
            .toLowerCase()
            .replace(/[^\w\s-]/g, '') // Remove special chars except hyphens
            .replace(/\s+/g, '-')     // Replace spaces with hyphens
            .replace(/-+/g, '-')      // Replace multiple hyphens with single
            .substring(0, this.config.entry_generation?.max_title_length || 50);
    }

    /**
     * Generate multiple scroll entries
     * @param {Array} parsedContents - Array of parsed content objects
     * @returns {Array} Array of generated scroll entries
     */
    generateMultipleScrolls(parsedContents) {
        return parsedContents
            .filter(content => content.lore_worthy)
            .map(content => this.generateScrollEntry(content));
    }

    /**
     * Get generation statistics
     * @returns {Object} Statistics object
     */
    getStats() {
        return { ...this.stats };
    }
}

// CLI interface
if (require.main === module) {
    const generator = new ScrollGenerator();
    
    // Test with mock parsed content
    const mockParsedContent = {
        type: 'issue',
        id: 17,
        title: '🧠 Feature Request: The Scribe System for TLDL',
        body: 'We need a self-perpetuating Scribe System for TLDL. The adventure begins...',
        author: 'jmeyer1980',
        created_at: '2025-01-15T10:00:00Z',
        category: 'feature_request',
        lore_worthy: true,
        priority: 'high',
        lore_content: ['We need a self-perpetuating Scribe System for TLDL', 'The adventure begins...'],
        tldl_tags: [],
        url: 'https://github.com/test/repo/issues/17'
    };

    const scroll = generator.generateScrollEntry(mockParsedContent);
    
    console.log('📜 Generated Scroll:');
    console.log('Filename:', scroll.filename);
    console.log('Metadata:', JSON.stringify(scroll.metadata, null, 2));
    console.log('\n--- Content Preview ---');
    console.log(scroll.content.substring(0, 500) + '...');
    
    console.log('\n📊 Generation Stats:');
    console.log(generator.getStats());
}

module.exports = ScrollGenerator;