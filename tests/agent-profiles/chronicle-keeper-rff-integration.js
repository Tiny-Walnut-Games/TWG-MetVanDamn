#!/usr/bin/env node
/**
 * Test script for RFF Protocol functionality
 */

const ScribeParser = require('./scribe-parser.js');

// Test 1: Content that should be lore-worthy (should not trigger RFF)
console.log('🧪 Test 1: Lore-worthy content (should pass)');
const parser1 = new ScribeParser();
const goodContent = {
    type: 'issue',
    number: 42,
    title: '🧠 Epic Quest Implementation: Adventure Framework',
    body: `**Phase**: Implementation
**KeeperNote**: This chronicles the implementation of the adventure framework, a critical system for the project.

📜 This discovery fundamentally changes how we approach user interactions, providing both performance improvements and enhanced user experience.

## Implementation Details
- Created new quest management system
- Optimized adventure algorithms  
- Added comprehensive logging

## Discoveries
- Performance improved by 200%
- User engagement metrics increased significantly
- Memory usage optimized through smart caching

## Next Adventures
- [ ] Expand to additional quest types
- [ ] Create performance monitoring dashboard
- [ ] Document patterns for future implementations`,
    author: 'test-user',
    created_at: '2025-01-15T10:00:00Z',
    state: 'open',
    labels: [{ name: 'enhancement' }],
    html_url: 'https://github.com/test/repo/issues/42'
};

const result1 = parser1.parseIssue(goodContent);
console.log('Result:', result1.lore_worthy ? '✅ Lore-worthy' : '❌ Not lore-worthy');
if (result1.rff_message) {
    console.log('🚨 Unexpected RFF triggered for good content!');
}
console.log('Stats:', parser1.getStats());
console.log('\n---\n');

// Test 2: Content that should trigger RFF (poor format)
console.log('🧪 Test 2: Poor content (should trigger RFF)');
const parser2 = new ScribeParser();
const badContent = {
    type: 'issue',
    number: 43,
    title: 'Small fix',
    body: 'Fixed a bug.',
    author: 'test-user',
    created_at: '2025-01-15T10:00:00Z',
    state: 'open',
    labels: [],
    html_url: 'https://github.com/test/repo/issues/43'
};

const result2 = parser2.parseIssue(badContent);
console.log('Result:', result2.lore_worthy ? '✅ Lore-worthy' : '❌ Not lore-worthy');
if (result2.rff_message) {
    console.log('✅ RFF Protocol activated correctly!');
    console.log('RFF ID:', result2.rff_message.rffId);
    console.log('Rejection reasons:', result2.rff_message.rejection.reasons);
    console.log('Keeper quote:', result2.rff_message.keeperQuote);
    console.log('Quick fixes:', result2.rff_message.guidance.quickFixes.slice(0, 2));
} else {
    console.log('🚨 RFF should have been triggered but was not!');
}
console.log('Stats:', parser2.getStats());
console.log('\n---\n');

// Test 3: Resubmission test
console.log('🧪 Test 3: Resubmission handling');
const parser3 = new ScribeParser();

// First, create a bad submission
const originalBad = {
    type: 'issue',
    number: 44,
    title: 'Update',
    body: 'Made some changes.',
    author: 'test-user',
    created_at: '2025-01-15T10:00:00Z',
    state: 'open',
    labels: [],
    html_url: 'https://github.com/test/repo/issues/44'
};

const originalResult = parser3.parseIssue(originalBad);
console.log('Original submission - RFF activated:', !!originalResult.rff_message);

// Now test a corrected resubmission
const correctedContent = {
    type: 'comment',
    id: 100,
    body: `🧠 TLDL: Corrected Implementation Update

**Phase**: Implementation  
**KeeperNote**: This correction addresses the RFF feedback and provides proper documentation for the adventure framework updates.

📜 The changes implement critical performance optimizations discovered during testing phase.

## What Changed
- Optimized database queries for 40% performance improvement
- Added comprehensive error handling
- Updated documentation with implementation notes

## Impact
- System response time improved significantly
- User experience enhanced through faster loading
- Maintenance complexity reduced through better code organization

## Next Steps
- [ ] Monitor performance metrics in production
- [ ] Gather user feedback on improvements  
- [ ] Plan next optimization phase`,
    author: 'test-user',
    created_at: '2025-01-15T10:01:00Z',
    html_url: 'https://github.com/test/repo/issues/44#issuecomment-100'
};

const resubmissionResult = parser3.parseComment(correctedContent);
console.log('Resubmission result - lore-worthy:', resubmissionResult.lore_worthy);
if (resubmissionResult.rff_message) {
    console.log('🚨 RFF still triggered - resubmission needs more work');
} else if (resubmissionResult.lore_worthy) {
    console.log('✅ Resubmission accepted for chronicle!');
}

console.log('Final stats:', parser3.getStats());