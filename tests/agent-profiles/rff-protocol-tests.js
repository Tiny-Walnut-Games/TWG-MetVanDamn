#!/usr/bin/env node
/**
 * Unit tests for RFF Protocol functionality
 * Tests the Request-for-Format Protocol system for Chronicle Keeper
 */

const assert = require('assert');
const RFFProtocol = require('../scripts/chronicle-keeper/rff-protocol.js');
const ScribeParser = require('../scripts/chronicle-keeper/scribe-parser.js');

class RFFProtocolTests {
    constructor() {
        this.testCount = 0;
        this.passCount = 0;
        this.failCount = 0;
    }

    /**
     * Assert helper with descriptive output
     */
    assert(testName, condition, message = '') {
        this.testCount++;
        try {
            assert(condition, message);
            console.log(`✅ ${testName}`);
            this.passCount++;
        } catch (error) {
            console.log(`❌ ${testName}: ${error.message}`);
            this.failCount++;
        }
    }

    /**
     * Test RFF Protocol rejection reason analysis
     */
    testRejectionAnalysis() {
        console.log('🧪 Testing RFF rejection reason analysis...');
        
        const rff = new RFFProtocol();
        
        // Test content with multiple issues
        const badContent = {
            type: 'issue',
            id: 123,
            title: 'Fix',
            body: 'Small change.',
            author: 'test-user'
        };
        
        const reasons = rff.analyzeRejectionReasons(badContent);
        
        this.assert('Detects missing TLDA header', reasons.includes('missing_tlda_header'));
        this.assert('Detects missing phase tag', reasons.includes('missing_phase_tag'));
        this.assert('Detects missing keeper note', reasons.includes('missing_keeper_note'));
        this.assert('Detects content too short', reasons.includes('content_too_short'));
        this.assert('Detects missing lore keywords', reasons.includes('missing_lore_keywords'));
    }

    /**
     * Test RFF message generation
     */
    testRFFMessageGeneration() {
        console.log('🧪 Testing RFF message generation...');
        
        const rff = new RFFProtocol();
        const content = {
            type: 'issue',
            id: 456,
            title: 'Bug fix',
            body: 'Fixed issue.',
            author: 'developer'
        };
        
        const rffMessage = rff.generateRFFMessage(content, 'content_too_short');
        
        this.assert('RFF message has ID', !!rffMessage.rffId);
        this.assert('RFF message has timestamp', !!rffMessage.timestamp);
        this.assert('RFF message has original content', !!rffMessage.originalContent);
        this.assert('RFF message has rejection details', !!rffMessage.rejection);
        this.assert('RFF message has guidance', !!rffMessage.guidance);
        this.assert('RFF message has examples', !!rffMessage.examples);
        this.assert('RFF message has resubmission prompt', !!rffMessage.resubmissionPrompt);
        this.assert('RFF message has keeper quote', !!rffMessage.keeperQuote);
        
        // Test guidance structure
        this.assert('Guidance has quick fixes', Array.isArray(rffMessage.guidance.quickFixes));
        this.assert('Guidance has detailed steps', Array.isArray(rffMessage.guidance.detailedSteps));
        this.assert('Guidance has best practices', Array.isArray(rffMessage.guidance.bestPractices));
    }

    /**
     * Test ScribeParser integration with RFF
     */
    testScribeParserRFFIntegration() {
        console.log('🧪 Testing ScribeParser RFF integration...');
        
        const parser = new ScribeParser();
        
        // Test content that should trigger RFF
        const poorContent = {
            type: 'issue',
            number: 789,
            title: 'Update',
            body: 'Changed some code.',
            author: 'test-dev',
            created_at: '2025-01-15T10:00:00Z',
            state: 'open',
            labels: [],
            html_url: 'https://github.com/test/repo/issues/789'
        };
        
        const result = parser.parseIssue(poorContent);
        
        this.assert('Poor content not lore-worthy', result.lore_worthy === false);
        this.assert('RFF message generated for poor content', !!result.rff_message);
        this.assert('RFF stats incremented', parser.getStats().rffIssued > 0);
        
        // Test content that should pass
        const goodContent = {
            type: 'issue',
            number: 790,
            title: '🧠 TLDA Quest Implementation: Enhanced Features',
            body: `**Phase**: Implementation
**KeeperNote**: This quest introduces critical new features for enhanced user experience.

📜 Adventure Details: Implementing comprehensive feature set for improved functionality.

## Implementation
- Added new quest management system
- Enhanced user interface components
- Optimized performance algorithms

## Discoveries
- User engagement improved by 150%
- System performance optimized significantly
- Code maintainability enhanced

## Next Adventures
- [ ] Deploy to production environment
- [ ] Gather user feedback metrics
- [ ] Plan next feature iteration`,
            author: 'quest-master',
            created_at: '2025-01-15T10:00:00Z',
            state: 'open',
            labels: [{ name: 'enhancement' }],
            html_url: 'https://github.com/test/repo/issues/790'
        };
        
        const goodResult = parser.parseIssue(goodContent);
        
        this.assert('Good content is lore-worthy', goodResult.lore_worthy === true);
        this.assert('No RFF for good content', !goodResult.rff_message);
    }

    /**
     * Test resubmission tracking
     */
    testResubmissionTracking() {
        console.log('🧪 Testing resubmission tracking...');
        
        const rff = new RFFProtocol();
        
        // Create original RFF
        const originalContent = {
            type: 'issue',
            id: 100,
            title: 'Fix',
            body: 'Fixed bug.',
            author: 'dev'
        };
        
        const originalRFF = rff.generateRFFMessage(originalContent);
        const rffId = originalRFF.rffId;
        
        // Test improved resubmission
        const improvedContent = {
            type: 'comment',
            id: 101,
            title: '🧠 TLDA Correction: Enhanced Bug Fix',
            body: `**Phase**: Implementation
**KeeperNote**: This corrected submission addresses the RFF feedback with proper documentation.

📜 Adventure Summary: Fixed critical bug affecting user authentication flow.

## Implementation Details
- Identified root cause in authentication middleware
- Implemented proper error handling
- Added comprehensive unit tests
- Updated documentation

## Impact
- User authentication success rate improved to 99.8%
- Error logging provides better debugging information
- System stability enhanced significantly

## Next Steps
- [ ] Monitor authentication metrics
- [ ] Conduct security audit
- [ ] Update related documentation`,
            author: 'dev'
        };
        
        const resubmissionResult = rff.trackResubmission(rffId, improvedContent);
        
        this.assert('Resubmission tracked', !!resubmissionResult.rffId);
        this.assert('Improvement detected', resubmissionResult.improved === true);
        this.assert('Ready for chronicle', resubmissionResult.readyForChronicle === true);
        this.assert('No remaining issues', resubmissionResult.remainingIssues.length === 0);
        
        // Test statistics
        const stats = rff.getStats();
        this.assert('RFF issued count correct', stats.rffIssued === 1);
        this.assert('Resubmissions count correct', stats.resubmissions === 1);
        this.assert('Successful count correct', stats.successful === 1);
        this.assert('Success rate calculated', stats.successRate === '100.0%');
    }

    /**
     * Test format examples generation
     */
    testFormatExamples() {
        console.log('🧪 Testing format examples generation...');
        
        const rff = new RFFProtocol();
        const reasons = ['missing_tlda_header', 'missing_phase_tag', 'missing_keeper_note', 'content_too_short'];
        const examples = rff.getFormatExamples(reasons);
        
        this.assert('TLDA header example provided', !!examples.tlda_header);
        this.assert('Phase tag example provided', !!examples.phase_tag);
        this.assert('Keeper note example provided', !!examples.keeper_note);
        this.assert('Rich content example provided', !!examples.rich_content);
        
        // Test example structure
        this.assert('Examples have titles', !!examples.tlda_header.title);
        this.assert('Examples have content', !!examples.tlda_header.content);
        this.assert('Examples contain brain emoji', examples.tlda_header.content.includes('🧠'));
    }

    /**
     * Run all tests
     */
    runAllTests() {
        console.log('🎯 Starting RFF Protocol Tests\n');
        
        try {
            this.testRejectionAnalysis();
            console.log('');
            
            this.testRFFMessageGeneration();
            console.log('');
            
            this.testScribeParserRFFIntegration();
            console.log('');
            
            this.testResubmissionTracking();
            console.log('');
            
            this.testFormatExamples();
            console.log('');
            
        } catch (error) {
            console.error('🚨 Test execution error:', error.message);
            this.failCount++;
        }
        
        // Print summary
        console.log('📊 Test Results Summary:');
        console.log(`Total Tests: ${this.testCount}`);
        console.log(`✅ Passed: ${this.passCount}`);
        console.log(`❌ Failed: ${this.failCount}`);
        
        if (this.failCount === 0) {
            console.log('\n🎉 All RFF Protocol tests passed! The system is ready for chronicle preservation.');
            process.exit(0);
        } else {
            console.log('\n💥 Some tests failed. Please review the RFF Protocol implementation.');
            process.exit(1);
        }
    }
}

// Run tests if this file is executed directly
if (require.main === module) {
    const tester = new RFFProtocolTests();
    tester.runAllTests();
}

module.exports = RFFProtocolTests;