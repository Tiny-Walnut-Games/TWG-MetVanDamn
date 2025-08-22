#!/usr/bin/env node
/**
 * Chronicle Keeper - Scribe Parser
 * 
 * Parses GitHub issues, comments, and PRs for lore-worthy content.
 * Extracts meaningful information for TLDL entry generation.
 */

const fs = require('fs');
const path = require('path');
const yaml = require('js-yaml');

class ScribeParser {
    constructor(configPath = null) {
        this.config = this.loadConfig(configPath);
        this.stats = {
            parsed: 0,
            loreWorthy: 0,
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
            return this.getDefaultConfig();
        }
    }

    getDefaultConfig() {
        return {
            scribe: { name: "Chronicle Keeper" },
            parsing: {
                lore_keywords: ["🧠", "🪶", "🍑", "📜", "adventure", "quest", "lore"],
                language_tags: {
                    javascript: ["// TLDL:", "/* TLDL:"],
                    python: ["# TLDL:", '"""TLDL:'],
                    bash: ["# TLDL:"]
                }
            },
            classification: {
                min_content_length: 50,
                categories: {
                    feature_request: { keywords: ["feature", "🧠"], priority: "high" },
                    bug_report: { keywords: ["bug", "issue"], priority: "medium" }
                }
            }
        };
    }

    /**
     * Parse GitHub issue content for TLDL worthiness
     * @param {Object} issue - GitHub issue object
     * @returns {Object} Parsed content with metadata
     */
    parseIssue(issue) {
        this.stats.parsed++;

        const content = {
            type: 'issue',
            id: issue.number,
            title: issue.title,
            body: issue.body || '',
            author: issue.user?.login || 'unknown',
            created_at: issue.created_at,
            updated_at: issue.updated_at,
            state: issue.state,
            labels: issue.labels?.map(l => l.name) || [],
            url: issue.html_url
        };

        // Extract lore-worthy content
        const loreContent = this.extractLoreContent(content);
        const category = this.classifyContent(content);
        const tldlTags = this.extractTldlTags(content.body);

        if (this.isLoreWorthy(content, loreContent)) {
            this.stats.loreWorthy++;
            return {
                ...content,
                lore_content: loreContent,
                category: category,
                tldl_tags: tldlTags,
                lore_worthy: true,
                priority: this.config.classification.categories[category]?.priority || 'medium'
            };
        }

        return { ...content, lore_worthy: false };
    }

    /**
     * Force content to be lore-worthy (for manual dispatch scenarios)
     * This method ensures stats are properly incremented when lore_worthy is manually set
     * @param {Object} parsedContent - Already parsed content
     * @returns {Object} Updated content with lore_worthy=true and incremented stats
     */
    forceAsLoreWorthy(parsedContent) {
        // Only increment stats if not already lore-worthy to avoid double counting
        if (!parsedContent.lore_worthy) {
            this.stats.loreWorthy++;
        }
        
        return {
            ...parsedContent,
            lore_worthy: true,
            category: parsedContent.category || 'brain_dump',
            priority: parsedContent.priority || 'medium'
        };
    }

    /**
     * Parse GitHub comment for TLDL worthiness
     * @param {Object} comment - GitHub comment object  
     * @returns {Object} Parsed content with metadata
     */
    parseComment(comment) {
        this.stats.parsed++;

        const content = {
            type: 'comment',
            id: comment.id,
            body: comment.body || '',
            author: comment.user?.login || 'unknown',
            created_at: comment.created_at,
            updated_at: comment.updated_at,
            url: comment.html_url,
            issue_number: comment.issue_url?.split('/').pop()
        };

        const loreContent = this.extractLoreContent(content);
        const tldlTags = this.extractTldlTags(content.body);

        if (this.isLoreWorthy(content, loreContent)) {
            this.stats.loreWorthy++;
            return {
                ...content,
                lore_content: loreContent,
                tldl_tags: tldlTags,
                lore_worthy: true,
                category: 'comment'
            };
        }

        return { ...content, lore_worthy: false };
    }

    /**
     * Parse GitHub Pull Request for TLDL worthiness
     * @param {Object} pr - GitHub PR object
     * @returns {Object} Parsed content with metadata
     */
    parsePullRequest(pr) {
        this.stats.parsed++;

        const content = {
            type: 'pull_request',
            id: pr.number,
            title: pr.title,
            body: pr.body || '',
            author: pr.user?.login || 'unknown',
            created_at: pr.created_at,
            updated_at: pr.updated_at,
            merged_at: pr.merged_at,
            state: pr.state,
            merged: pr.merged,
            base_branch: pr.base?.ref,
            head_branch: pr.head?.ref,
            url: pr.html_url,
            commits: pr.commits,
            additions: pr.additions,
            deletions: pr.deletions,
            changed_files: pr.changed_files
        };

        const loreContent = this.extractLoreContent(content);
        const category = this.classifyContent(content);
        const tldlTags = this.extractTldlTags(content.body);

        if (this.isLoreWorthy(content, loreContent)) {
            this.stats.loreWorthy++;
            return {
                ...content,
                lore_content: loreContent,
                category: category,
                tldl_tags: tldlTags,
                lore_worthy: true,
                priority: content.merged ? 'high' : 'medium'
            };
        }

        return { ...content, lore_worthy: false };
    }

    /**
     * Extract lore-worthy content from text
     * @param {Object} content - Content object with title/body
     * @returns {Array} Array of lore-worthy excerpts
     */
    extractLoreContent(content) {
        const text = `${content.title || ''} ${content.body || ''}`;
        const loreContent = [];
        const keywords = this.config.parsing.lore_keywords;

        // Find sentences containing lore keywords
        const sentences = text.split(/[.!?]+/).filter(s => s.trim());
        
        for (const sentence of sentences) {
            const lowerSentence = sentence.toLowerCase();
            for (const keyword of keywords) {
                if (lowerSentence.includes(keyword.toLowerCase()) || sentence.includes(keyword)) {
                    loreContent.push(sentence.trim());
                    break;
                }
            }
        }

        // Extract quoted text (often contains wisdom)
        const quotes = text.match(/["'""]/g);
        if (quotes) {
            const quotedText = text.match(/["'""](.*?)["'""]/g);
            if (quotedText) {
                loreContent.push(...quotedText.map(q => q.replace(/["'""]/g, '').trim()));
            }
        }

        // Extract emoji-prefixed lines (often important)
        const emojiLines = text.split('\n').filter(line => /^[🧠🪶🍑📜🧙‍♂️🎯🛡️]/.test(line.trim()));
        loreContent.push(...emojiLines.map(line => line.trim()));

        return [...new Set(loreContent)]; // Remove duplicates
    }

    /**
     * Extract TLDL tags from content
     * @param {string} text - Text to search for TLDL tags
     * @returns {Array} Array of TLDL tag objects
     */
    extractTldlTags(text) {
        const tags = [];
        const languageTags = this.config.parsing.language_tags;

        for (const [language, tagPatterns] of Object.entries(languageTags)) {
            for (const tagPattern of tagPatterns) {
                const regex = new RegExp(`${this.escapeRegex(tagPattern)}\\s*(.+?)(?:\\n|$)`, 'gi');
                let match;
                
                while ((match = regex.exec(text)) !== null) {
                    tags.push({
                        language: language,
                        tag_pattern: tagPattern,
                        content: match[1].trim(),
                        full_match: match[0].trim()
                    });
                }
            }
        }

        return tags;
    }

    /**
     * Classify content into categories
     * @param {Object} content - Content to classify
     * @returns {string} Category name
     */
    classifyContent(content) {
        const text = `${content.title || ''} ${content.body || ''}`.toLowerCase();
        const categories = this.config.classification.categories;

        for (const [category, rules] of Object.entries(categories)) {
            for (const keyword of rules.keywords) {
                if (text.includes(keyword.toLowerCase())) {
                    return category;
                }
            }
        }

        // Default classification based on type
        if (content.type === 'pull_request') return 'workflow';
        if (content.type === 'comment') return 'discussion';
        return 'general';
    }

    /**
     * Determine if content is worthy of TLDL entry
     * @param {Object} content - Original content
     * @param {Array} loreContent - Extracted lore content
     * @returns {boolean} True if worthy of TLDL
     */
    isLoreWorthy(content) {
        const text = `${content.title || ''} ${content.body || ''}`;
        
        // Minimum length check
        if (text.length < this.config.classification.min_content_length) {
            return false;
        }

        // Check for bot users (usually not lore-worthy)
        if (this.config.github?.bot_users?.includes(content.author)) {
            return false;
        }

        // Check for lore keywords
        const keywords = this.config.parsing.lore_keywords;
        const lowerText = text.toLowerCase();
        
        for (const keyword of keywords) {
            if (lowerText.includes(keyword.toLowerCase()) || text.includes(keyword)) {
                return true;
            }
        }

        // Check for TLDL tags
        if (this.extractTldlTags(text).length > 0) {
            return true;
        }

        // Special cases for high-value content
        if (content.type === 'issue' && content.labels?.includes('enhancement')) return true;
        if (content.type === 'pull_request' && content.merged) return true;
        if (content.type === 'issue' && content.title?.includes('Feature Request')) return true;

        return false;
    }

    /**
     * Parse workflow run data for TLDL worthiness
     * @param {Object} workflowRun - GitHub workflow run object
     * @returns {Object} Parsed workflow data
     */
    parseWorkflowRun(workflowRun) {
        this.stats.parsed++;

        const content = {
            type: 'workflow_run',
            id: workflowRun.id,
            name: workflowRun.name,
            status: workflowRun.status,
            conclusion: workflowRun.conclusion,
            created_at: workflowRun.created_at,
            updated_at: workflowRun.updated_at,
            head_branch: workflowRun.head_branch,
            head_sha: workflowRun.head_sha,
            url: workflowRun.html_url,
            run_number: workflowRun.run_number
        };

        // Workflow runs are lore-worthy if they fail or are particularly significant
        const loreWorthy = content.conclusion === 'failure' || 
                          content.name.includes('deploy') ||
                          content.name.includes('release');

        if (loreWorthy) {
            this.stats.loreWorthy++;
            return {
                ...content,
                lore_worthy: true,
                category: 'workflow',
                priority: content.conclusion === 'failure' ? 'high' : 'medium'
            };
        }

        return { ...content, lore_worthy: false };
    }

    /**
     * Get parsing statistics
     * @returns {Object} Statistics object
     */
    getStats() {
        return {
            ...this.stats,
            lore_percentage: this.stats.parsed > 0 ? 
                Math.round((this.stats.loreWorthy / this.stats.parsed) * 100) : 0
        };
    }

    /**
     * Escape special regex characters
     * @param {string} string - String to escape
     * @returns {string} Escaped string
     */
    escapeRegex(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    /**
     * Parse multiple GitHub objects
     * @param {Array} items - Array of GitHub objects (issues, comments, PRs)
     * @returns {Array} Array of parsed results
     */
    parseMultiple(items) {
        const results = [];
        
        for (const item of items) {
            try {
                let parsed;
                
                if (item.pull_request) {
                    parsed = this.parsePullRequest(item);
                } else if (item.issue_url || item.body !== undefined) {
                    parsed = this.parseComment(item);
                } else if (item.number !== undefined) {
                    parsed = this.parseIssue(item);
                } else if (item.workflow_id !== undefined) {
                    parsed = this.parseWorkflowRun(item);
                }
                
                if (parsed) {
                    results.push(parsed);
                }
            } catch (error) {
                this.stats.errors++;
                console.error(`🚨 Error parsing item ${item.id}:`, error.message);
            }
        }

        return results;
    }
}

// CLI interface
if (require.main === module) {
    const parser = new ScribeParser();
    
    // Example usage with mock data
    const mockIssue = {
        number: 17,
        title: "🧠 Feature Request: The Scribe System for TLDL", 
        body: "We need a self-perpetuating Scribe System for TLDL. The adventure begins...",
        user: { login: "jmeyer1980" },
        created_at: "2025-01-15T10:00:00Z",
        state: "open",
        labels: [{ name: "enhancement" }],
        html_url: "https://github.com/test/repo/issues/17"
    };

    const result = parser.parseIssue(mockIssue);
    console.log("📜 Chronicle Keeper Parse Result:");
    console.log(JSON.stringify(result, null, 2));
    
    console.log("\n📊 Chronicle Keeper Stats:");
    console.log(parser.getStats());
}

module.exports = ScribeParser;