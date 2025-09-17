#!/usr/bin/env node
/**
 * Test: Workflow Cooldown System
 * 
 * Tests the cooldown mechanism to ensure it prevents infinite loops
 * while allowing legitimate use cases.
 */

const fs = require('fs');
const path = require('path');
const { WorkflowCooldown } = require('../scripts/cid-faculty/shared/workflow-cooldown.js');

// Test helper functions
function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function cleanupTestFiles(cooldownDir) {
    if (fs.existsSync(cooldownDir)) {
        fs.rmSync(cooldownDir, { recursive: true, force: true });
    }
}

// Test suite
async function runCooldownTests() {
    console.log('🧪 Starting Workflow Cooldown Tests...\n');
    
    const testCooldownDir = '/tmp/test-cooldown';
    cleanupTestFiles(testCooldownDir);
    
    let testsPassed = 0;
    let testsTotal = 0;
    
    // Test 1: Basic cooldown functionality
    testsTotal++;
    console.log('Test 1: Basic cooldown functionality');
    try {
        const cooldown = new WorkflowCooldown(testCooldownDir, 1000); // 1 second cooldown
        const workflow = 'test-workflow';
        const issue = '123';
        
        // Initially no cooldown
        let result = cooldown.isInCooldown(workflow, issue);
        if (result !== false) {
            throw new Error('Expected no initial cooldown');
        }
        
        // Set cooldown
        cooldown.setCooldown(workflow, issue, { type: 'test', pattern: 'test' });
        
        // Should be in cooldown
        result = cooldown.isInCooldown(workflow, issue);
        if (!result || !result.inCooldown) {
            throw new Error('Expected cooldown to be active');
        }
        
        // Wait for cooldown to expire
        await delay(1100);
        
        // Should no longer be in cooldown
        result = cooldown.isInCooldown(workflow, issue);
        if (result !== false) {
            throw new Error('Expected cooldown to have expired');
        }
        
        testsPassed++;
        console.log('✅ PASS\n');
    } catch (error) {
        console.log(`❌ FAIL: ${error.message}\n`);
    }
    
    // Test 2: Loop detection
    testsTotal++;
    console.log('Test 2: Loop detection');
    try {
        const cooldown = new WorkflowCooldown(testCooldownDir);
        const triggerPatterns = ['TLDL:', '🔮', '📜'];
        
        // Test text that would trigger a loop
        let result = cooldown.wouldTriggerLoop('test-workflow', triggerPatterns, 'Here is some TLDL: content that would trigger');
        if (!result.wouldLoop || !result.patterns.includes('TLDL:')) {
            throw new Error('Expected loop detection for TLDL: pattern');
        }
        
        // Test text that would not trigger a loop
        result = cooldown.wouldTriggerLoop('test-workflow', triggerPatterns, 'Normal comment without triggers');
        if (result.wouldLoop) {
            throw new Error('Expected no loop detection for normal text');
        }
        
        // Test multiple patterns (high risk)
        result = cooldown.wouldTriggerLoop('test-workflow', triggerPatterns, 'TLDL: something with 🔮 multiple triggers');
        if (!result.wouldLoop || result.risk !== 'high') {
            throw new Error('Expected high risk detection for multiple patterns');
        }
        
        testsPassed++;
        console.log('✅ PASS\n');
    } catch (error) {
        console.log(`❌ FAIL: ${error.message}\n`);
    }
    
    // Test 3: Execution count tracking
    testsTotal++;
    console.log('Test 3: Execution count tracking');
    try {
        const cooldown = new WorkflowCooldown(testCooldownDir, 500); // 0.5 second cooldown
        const workflow = 'test-workflow';
        const issue = '456';
        
        // First execution
        cooldown.setCooldown(workflow, issue, { type: 'test', pattern: 'test' });
        let status = cooldown.getCooldownStatus(workflow, issue);
        if (status.executionCount !== 1) {
            throw new Error('Expected execution count of 1');
        }
        
        // Wait for cooldown to expire
        await delay(600);
        
        // Second execution (should increment count)
        cooldown.setCooldown(workflow, issue, { type: 'test', pattern: 'test' });
        status = cooldown.getCooldownStatus(workflow, issue);
        if (status.executionCount !== 2) {
            throw new Error('Expected execution count of 2');
        }
        
        testsPassed++;
        console.log('✅ PASS\n');
    } catch (error) {
        console.log(`❌ FAIL: ${error.message}\n`);
    }
    
    // Test 4: Different cooldown periods
    testsTotal++;
    console.log('Test 4: Different cooldown periods');
    try {
        const cooldown = new WorkflowCooldown(testCooldownDir);
        const workflow = 'test-workflow';
        const issue = '789';
        
        // Set custom cooldown
        cooldown.setCooldown(workflow, issue, { type: 'test', pattern: 'test' }, 800); // 0.8 seconds
        
        let result = cooldown.isInCooldown(workflow, issue);
        if (!result || !result.inCooldown) {
            throw new Error('Expected cooldown to be active');
        }
        
        // Wait less than cooldown period
        await delay(400);
        result = cooldown.isInCooldown(workflow, issue);
        if (!result || !result.inCooldown) {
            throw new Error('Expected cooldown to still be active');
        }
        
        // Wait for full cooldown period
        await delay(500);
        result = cooldown.isInCooldown(workflow, issue);
        if (result !== false) {
            throw new Error('Expected cooldown to have expired');
        }
        
        testsPassed++;
        console.log('✅ PASS\n');
    } catch (error) {
        console.log(`❌ FAIL: ${error.message}\n`);
    }
    
    // Test 5: Statistics and cleanup
    testsTotal++;
    console.log('Test 5: Statistics and cleanup');
    try {
        const cooldown = new WorkflowCooldown(testCooldownDir);
        
        // Create some test cooldowns
        cooldown.setCooldown('workflow1', '100', { type: 'test' });
        cooldown.setCooldown('workflow2', '200', { type: 'test' });
        
        let stats = cooldown.getStats();
        if (stats.files < 2 || stats.activeCooldowns < 2) {
            throw new Error('Expected at least 2 active cooldowns');
        }
        
        // Test cleanup
        cooldown.cleanup(0); // Clean files older than 0 hours (all files)
        stats = cooldown.getStats();
        if (stats.files !== 0) {
            throw new Error('Expected all files to be cleaned up');
        }
        
        testsPassed++;
        console.log('✅ PASS\n');
    } catch (error) {
        console.log(`❌ FAIL: ${error.message}\n`);
    }
    
    // Cleanup
    cleanupTestFiles(testCooldownDir);
    
    // Summary
    console.log('🧪 Test Results:');
    console.log(`✅ Passed: ${testsPassed}/${testsTotal}`);
    console.log(`❌ Failed: ${testsTotal - testsPassed}/${testsTotal}`);
    
    if (testsPassed === testsTotal) {
        console.log('🎉 All tests passed! Cooldown system is working correctly.');
        process.exit(0);
    } else {
        console.log('💥 Some tests failed. Please review the implementation.');
        process.exit(1);
    }
}

// Run tests
if (require.main === module) {
    runCooldownTests().catch(error => {
        console.error(`❌ Test error: ${error.message}`);
        process.exit(1);
    });
}

module.exports = { runCooldownTests };