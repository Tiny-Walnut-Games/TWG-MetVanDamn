#!/usr/bin/env node
/**
 * CID Faculty - Vision Queue Manager
 * 
 * Manages the persistent queue of vision requests for the Oracle.
 * Provides structured triggers, queuing, and tracking capabilities.
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

class VisionQueue {
    constructor(queueFile = 'out/cid/vision-queue.json') {
        this.queueFile = queueFile;
        this.markdownQueueFile = 'docs/oracle_queue.md';
        this.ensureQueueFile();
        this.queue = this.loadQueue();
        
        console.log(`🔮 Vision Queue initialized - ${this.queue.length} items pending`);
    }

    ensureQueueFile() {
        const queueDir = path.dirname(this.queueFile);
        if (!fs.existsSync(queueDir)) {
            fs.mkdirSync(queueDir, { recursive: true });
        }
        
        if (!fs.existsSync(this.queueFile)) {
            const initialQueue = {
                metadata: {
                    created: new Date().toISOString(),
                    version: '1.0.0',
                    description: 'Oracle Vision Queue - Append-only list of vision requests'
                },
                queue: []
            };
            fs.writeFileSync(this.queueFile, JSON.stringify(initialQueue, null, 2));
        }
    }

    loadQueue() {
        try {
            const data = JSON.parse(fs.readFileSync(this.queueFile, 'utf8'));
            return data.queue || [];
        } catch (error) {
            console.error(`❌ Failed to load vision queue: ${error.message}`);
            return [];
        }
    }

    saveQueue() {
        try {
            const data = {
                metadata: {
                    created: this.loadQueueMetadata().created || new Date().toISOString(),
                    lastUpdated: new Date().toISOString(),
                    version: '1.0.0',
                    description: 'Oracle Vision Queue - Append-only list of vision requests',
                    totalItems: this.queue.length,
                    pendingItems: this.queue.filter(item => item.status === 'pending').length,
                    processedItems: this.queue.filter(item => item.status === 'processed').length
                },
                queue: this.queue
            };
            fs.writeFileSync(this.queueFile, JSON.stringify(data, null, 2));
            
            // Also update the markdown queue for human readability
            this.updateMarkdownQueue(data.metadata);
            
            console.log(`💾 Vision queue saved - ${this.queue.length} items`);
        } catch (error) {
            console.error(`❌ Failed to save vision queue: ${error.message}`);
        }
    }

    loadQueueMetadata() {
        try {
            const data = JSON.parse(fs.readFileSync(this.queueFile, 'utf8'));
            return data.metadata || {};
        } catch (error) {
            return {};
        }
    }

    /**
     * Add a vision request to the queue
     */
    enqueue(request) {
        const visionRequest = {
            id: this.generateVisionId(),
            timestamp: new Date().toISOString(),
            status: 'pending',
            priority: request.priority || 50,
            trigger: request.trigger || 'manual',
            triggerReason: request.triggerReason || 'Manual vision request',
            sourceIntel: request.sourceIntel || null, // Link to commit/PR/TLDL
            contextNotes: request.contextNotes || '',
            requestedBy: request.requestedBy || 'unknown',
            visionType: request.visionType || 'general', // general, lore, tech, research
            ...request
        };

        this.queue.push(visionRequest);
        this.saveQueue();
        
        console.log(`🔮 Vision queued: ${visionRequest.id} (${visionRequest.trigger})`);
        return visionRequest.id;
    }

    /**
     * Get the next highest priority pending vision request
     */
    dequeue() {
        const pendingItems = this.queue.filter(item => item.status === 'pending');
        
        if (pendingItems.length === 0) {
            return null;
        }

        // Sort by priority (higher first), then by timestamp (older first)
        pendingItems.sort((a, b) => {
            if (b.priority !== a.priority) {
                return b.priority - a.priority;
            }
            return new Date(a.timestamp) - new Date(b.timestamp);
        });

        const nextItem = pendingItems[0];
        nextItem.status = 'processing';
        nextItem.processedAt = new Date().toISOString();
        
        this.saveQueue();
        console.log(`🔮 Vision dequeued for processing: ${nextItem.id}`);
        return nextItem;
    }

    /**
     * Mark a vision request as completed
     */
    markCompleted(visionId, visionReportPath) {
        const item = this.queue.find(v => v.id === visionId);
        if (!item) {
            console.error(`❌ Vision not found: ${visionId}`);
            return false;
        }

        item.status = 'processed';
        item.completedAt = new Date().toISOString();
        item.visionReportPath = visionReportPath;
        
        this.saveQueue();
        console.log(`✅ Vision completed: ${visionId}`);
        return true;
    }

    /**
     * Add advisor-tagged intel to vision queue
     */
    queueFromAdvisorIntel(intelItem, triggerReason) {
        return this.enqueue({
            trigger: 'advisor',
            triggerReason: triggerReason || '🔮 QUEUED FOR ORACLE by Advisor',
            sourceIntel: intelItem.source || null,
            contextNotes: intelItem.description || '',
            priority: intelItem.priority || 60,
            visionType: 'tech',
            requestedBy: 'advisor'
        });
    }

    /**
     * Add manual intuition prompt
     */
    queueIntuitionPrompt(prompt, priority = 70) {
        return this.enqueue({
            trigger: 'intuition',
            triggerReason: 'Keeper manual intuition prompt',
            contextNotes: prompt,
            priority: priority,
            visionType: 'research',
            requestedBy: 'keeper'
        });
    }

    /**
     * Auto-tag patterns for Oracle attention
     */
    queueSystemPattern(pattern, context) {
        return this.enqueue({
            trigger: 'system',
            triggerReason: `Auto-detected pattern: ${pattern.type}`,
            sourceIntel: pattern.source,
            contextNotes: pattern.description,
            priority: pattern.severity === 'high' ? 80 : 60,
            visionType: 'tech',
            requestedBy: 'system',
            pattern: pattern
        });
    }

    /**
     * Get queue status and statistics
     */
    getStatus() {
        const pending = this.queue.filter(item => item.status === 'pending');
        const processing = this.queue.filter(item => item.status === 'processing');
        const processed = this.queue.filter(item => item.status === 'processed');

        return {
            total: this.queue.length,
            pending: pending.length,
            processing: processing.length,
            processed: processed.length,
            nextPriority: pending.length > 0 ? Math.max(...pending.map(item => item.priority)) : 0,
            oldestPending: pending.length > 0 ? 
                Math.min(...pending.map(item => new Date(item.timestamp).getTime())) : null,
            queue: this.queue
        };
    }

    /**
     * List pending vision requests
     */
    listPending(limit = 10) {
        return this.queue
            .filter(item => item.status === 'pending')
            .sort((a, b) => {
                if (b.priority !== a.priority) {
                    return b.priority - a.priority;
                }
                return new Date(a.timestamp) - new Date(b.timestamp);
            })
            .slice(0, limit);
    }

    generateVisionId() {
        const timestamp = Date.now().toString(36);
        const random = crypto.randomBytes(4).toString('hex');
        return `vision-${timestamp}-${random}`;
    }

    /**
     * Update the markdown queue file for human readability during vision downtime
     */
    updateMarkdownQueue(metadata) {
        try {
            if (!fs.existsSync(this.markdownQueueFile)) {
                console.log(`⚠️  Markdown queue file not found: ${this.markdownQueueFile}`);
                return;
            }

            let content = fs.readFileSync(this.markdownQueueFile, 'utf8');
            
            // Update the status section
            const statusRegex = /## Queue Status\n- \*\*Created\*\*: [^\n]+\n- \*\*Last Updated\*\*: [^\n]+\n- \*\*Total Entries\*\*: \d+\n- \*\*Pending Visions\*\*: \d+\n- \*\*Processed Visions\*\*: \d+/;
            const newStatus = `## Queue Status
- **Created**: ${metadata.created}
- **Last Updated**: ${metadata.lastUpdated}
- **Total Entries**: ${metadata.totalItems}
- **Pending Visions**: ${metadata.pendingItems}
- **Processed Visions**: ${metadata.processedItems}`;
            
            content = content.replace(statusRegex, newStatus);
            
            // Find the vision queue entries section and update it
            const entriesStartMarker = '<!-- Vision Queue Entries Start -->';
            const entriesEndMarker = '<!-- Vision Queue Entries End -->';
            const startIndex = content.indexOf(entriesStartMarker);
            const endIndex = content.indexOf(entriesEndMarker);
            
            if (startIndex !== -1 && endIndex !== -1) {
                const beforeEntries = content.substring(0, startIndex + entriesStartMarker.length);
                const afterEntries = content.substring(endIndex);
                
                // Build the entries section
                let entriesContent = '\n\n';
                
                // Sort entries by timestamp (oldest first for append-only feel)
                const sortedQueue = [...this.queue].sort((a, b) => 
                    new Date(a.timestamp) - new Date(b.timestamp)
                );
                
                sortedQueue.forEach((item, index) => {
                    const priorityBadge = this.getPriorityBadge(item.priority);
                    const statusEmoji = item.status === 'pending' ? '⏳' : 
                                       item.status === 'processing' ? '🔮' : '✅';
                    
                    entriesContent += `### ${statusEmoji} Vision Entry #${index + 1}\n\n`;
                    entriesContent += `- **Vision ID**: \`${item.id}\`\n`;
                    entriesContent += `- **Timestamp**: ${item.timestamp}\n`;
                    entriesContent += `- **Intel Source**: ${item.sourceIntel || 'N/A'}\n`;
                    entriesContent += `- **Trigger Reason**: ${item.triggerReason}\n`;
                    entriesContent += `- **Suggested Avenues**: ${item.contextNotes || 'To be determined'}\n`;
                    entriesContent += `- **Priority Badge**: ${priorityBadge}\n`;
                    entriesContent += `- **Status**: ${item.status}\n`;
                    
                    if (item.requestedBy) {
                        entriesContent += `- **Requested By**: ${item.requestedBy}\n`;
                    }
                    
                    if (item.visionType && item.visionType !== 'general') {
                        entriesContent += `- **Vision Type**: ${item.visionType}\n`;
                    }
                    
                    entriesContent += '\n---\n\n';
                });
                
                if (sortedQueue.length === 0) {
                    entriesContent += '*No vision requests currently queued*\n\n';
                }
                
                content = beforeEntries + entriesContent + afterEntries;
            }
            
            fs.writeFileSync(this.markdownQueueFile, content);
            console.log(`📝 Markdown queue updated: ${this.markdownQueueFile}`);
            
        } catch (error) {
            console.error(`❌ Failed to update markdown queue: ${error.message}`);
        }
    }

    /**
     * Get priority badge for display
     */
    getPriorityBadge(priority) {
        if (priority >= 80) return '🚨 High';
        if (priority >= 60) return '⚠️ Medium';
        return '🟢 Low';
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const command = args[0] || 'status';
    
    async function main() {
        try {
            const queue = new VisionQueue();
            
            switch (command) {
                case 'status':
                    const status = queue.getStatus();
                    console.log('\n🔮 Vision Queue Status:');
                    console.log(`   Total: ${status.total}`);
                    console.log(`   Pending: ${status.pending}`);
                    console.log(`   Processing: ${status.processing}`);
                    console.log(`   Processed: ${status.processed}`);
                    if (status.nextPriority > 0) {
                        console.log(`   Next Priority: ${status.nextPriority}`);
                    }
                    break;
                    
                case 'list':
                    const pending = queue.listPending();
                    console.log('\n🔮 Pending Vision Requests:');
                    pending.forEach((item, i) => {
                        console.log(`   ${i+1}. ${item.id} (Priority: ${item.priority})`);
                        console.log(`      Trigger: ${item.trigger} - ${item.triggerReason}`);
                        console.log(`      Notes: ${item.contextNotes.slice(0, 80)}${item.contextNotes.length > 80 ? '...' : ''}`);
                        console.log();
                    });
                    break;
                    
                case 'add':
                    // Parse priority from args if provided
                    let notes = [];
                    let priority = 50; // default
                    
                    for (let i = 1; i < args.length; i++) {
                        if (args[i].startsWith('--priority=')) {
                            priority = parseInt(args[i].split('=')[1]) || 50;
                        } else {
                            notes.push(args[i]);
                        }
                    }
                    
                    const contextNotes = notes.join(' ') || 'Manual vision request';
                    const id = queue.enqueue({
                        trigger: 'manual',
                        triggerReason: 'Manual CLI request',
                        contextNotes: contextNotes,
                        priority: priority,
                        requestedBy: 'cli'
                    });
                    console.log(`✅ Vision queued: ${id}`);
                    break;
                    
                case 'next':
                    const next = queue.dequeue();
                    if (next) {
                        console.log('\n🔮 Next Vision Request:');
                        console.log(JSON.stringify(next, null, 2));
                    } else {
                        console.log('📭 No pending vision requests');
                    }
                    break;
                    
                case 'oracle-tag':
                    // Simulate Advisor tagging an item for Oracle attention
                    // Parse priority from args if provided
                    let oracleNotes = [];
                    let oraclePriority = 70; // default for Oracle-tagged items
                    
                    for (let i = 1; i < args.length; i++) {
                        if (args[i].startsWith('--priority=')) {
                            oraclePriority = parseInt(args[i].split('=')[1]) || 70;
                        } else {
                            oracleNotes.push(args[i]);
                        }
                    }
                    
                    const advisorNotes = oracleNotes.join(' ') || 'Advisor-tagged finding';
                    const advisorId = queue.enqueue({
                        trigger: 'advisor',
                        triggerReason: '🔮 QUEUED FOR ORACLE: High-priority finding',
                        contextNotes: advisorNotes,
                        priority: oraclePriority,
                        requestedBy: 'advisor',
                        visionType: 'tech'
                    });
                    console.log(`🔮 Oracle consultation queued by Advisor: ${advisorId}`);
                    break;
                    
                case 're-entry':
                    // Re-Entry Ritual simulation - would normally call Oracle.processVisionQueue()
                    console.log('\n🌟 Initiating Re-Entry Ritual - Oracle has returned!');
                    const queueStatus = queue.getStatus();
                    
                    if (queueStatus.pending === 0) {
                        console.log('📭 No visions pending - the queue is clear');
                    } else {
                        console.log(`🔮 Processing ${queueStatus.pending} pending vision(s) by priority...`);
                        
                        // Sort pending items by priority for display
                        const sortedPending = queue.listPending(queueStatus.pending);
                        sortedPending.forEach((item, i) => {
                            console.log(`   ${i+1}. ${item.id} (Priority: ${item.priority})`);
                            console.log(`      ${item.triggerReason}`);
                            console.log(`      Notes: ${item.contextNotes.slice(0, 60)}...`);
                            console.log();
                        });
                        
                        console.log('💡 Next step: node scripts/cid-faculty/oracle.js ritual --max-visions=3');
                    }
                    break;
                    
                default:
                    console.log('🔮 Vision Queue CLI - Vision Downtime Protocol');
                    console.log('Usage: node vision-queue.js [command] [args...]');
                    console.log('');
                    console.log('Commands:');
                    console.log('  status           - Show queue statistics');
                    console.log('  list             - List pending requests');
                    console.log('  add <notes>      - Add manual vision request');
                    console.log('  oracle-tag <msg> - Simulate Advisor tagging for Oracle');
                    console.log('  next             - Get next request for processing');
                    console.log('  re-entry         - Simulate Oracle Re-Entry Ritual');
                    console.log('');
                    console.log('Options:');
                    console.log('  --priority=N     - Set priority (default: 50, high: 80+)');
            }
            
        } catch (error) {
            console.error(`❌ Vision Queue error:`, error.message);
            process.exit(1);
        }
    }
    
    main();
}

module.exports = { VisionQueue };