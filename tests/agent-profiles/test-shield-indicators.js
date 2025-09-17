#!/usr/bin/env node
/**
 * Pass-by-Fail Shield Indicator System - Test Suite
 * 
 * Tests the shield detection and badge application functionality.
 */

const ShieldIndicatorSystem = require('../scripts/cid-schoolhouse/shield-indicators.js');
const BadgeSystem = require('../scripts/cid-schoolhouse/badges.js');

class ShieldIndicatorTests {
    constructor() {
        this.shieldSystem = new ShieldIndicatorSystem();
        this.badgeSystem = new BadgeSystem();
        this.tests = [];
        this.passed = 0;
        this.failed = 0;
    }

    runAllTests() {
        console.log('🛡 PASS-BY-FAIL SHIELD INDICATOR TESTS');
        console.log('====================================');
        console.log('');

        this.testShieldDetection();
        this.testBadgeSystemIntegration();
        this.testWorkflowAnalysis();
        this.testShieldSummaryGeneration();

        this.printResults();
    }

    testShieldDetection() {
        console.log('🔍 Testing Shield Detection Logic...');
        
        // Test 1: Keeper's Shield detection
        const keeperContext = {
            jobName: 'keeper-shield-test',
            logs: 'Guard tripwire activated - archive integrity preserved',
            errorMessage: 'Keeper shield defensive mechanism triggered',
            workflow: 'Archive Protection'
        };
        
        const keeperResult = this.badgeSystem.detectShieldStatus(keeperContext);
        this.assert('Keeper Shield Detection', keeperResult.isShield === true && keeperResult.badgeType === 'Keeper\'s Shield');

        // Test 2: Bug of Honor detection
        const bugHonorContext = {
            jobName: 'bug-of-honor-test',
            logs: 'Bug of honor activated - feature wearing bug coat',
            errorMessage: 'Intentional bug for system protection',
            workflow: 'Honor System'
        };
        
        const bugResult = this.badgeSystem.detectShieldStatus(bugHonorContext);
        this.assert('Bug of Honor Detection', bugResult.isShield === true && bugResult.badgeType === 'Bug of Honor');

        // Test 3: Buttsafe detection
        const buttsafeContext = {
            jobName: 'buttsafe-protection',
            logs: 'Cheek preservation protocol activated successfully',
            errorMessage: 'Buttsafe triggered for scroll lineage protection',
            workflow: 'Cheek Guardian'
        };
        
        const buttsafeResult = this.badgeSystem.detectShieldStatus(buttsafeContext);
        this.assert('Buttsafe Detection', buttsafeResult.isShield === true && buttsafeResult.badgeType === 'Buttsafe Triggered');

        // Test 4: Pass-by-Fail detection
        const passByFailContext = {
            jobName: 'pass-by-fail-test',
            logs: 'Expected pass by fail mechanism engaged',
            errorMessage: 'This failure is actually a pass',
            workflow: 'Pass by Fail Demo'
        };
        
        const passFailResult = this.badgeSystem.detectShieldStatus(passByFailContext);
        this.assert('Pass-by-Fail Detection', passFailResult.isShield === true && passFailResult.badgeType === 'Pass-by-Fail');

        // Test 5: No shield detection for normal failure
        const normalContext = {
            jobName: 'regular-test',
            logs: 'Regular test failure occurred',
            errorMessage: 'Actual bug found in code',
            workflow: 'Normal CI'
        };
        
        const normalResult = this.badgeSystem.detectShieldStatus(normalContext);
        this.assert('Normal Failure (No Shield)', normalResult.isShield === false);
    }

    testBadgeSystemIntegration() {
        console.log('🏆 Testing Badge System Integration...');
        
        // Test badge emoji mapping
        this.assert('Keeper Shield Emoji', this.badgeSystem.badgeEmojis['Keeper\'s Shield'] === '🛡');
        this.assert('Bug of Honor Emoji', this.badgeSystem.badgeEmojis['Bug of Honor'] === '🛡');
        this.assert('Buttsafe Triggered Emoji', this.badgeSystem.badgeEmojis['Buttsafe Triggered'] === '🛡');
        this.assert('Pass-by-Fail Emoji', this.badgeSystem.badgeEmojis['Pass-by-Fail'] === '🛡');
        
        // Test shield badge formatting
        const shieldBadges = ['Keeper\'s Shield', 'Bug of Honor'];
        const comment = this.badgeSystem.formatBadgeComment(shieldBadges);
        
        this.assert('Shield Badge Formatting', comment.includes('🛡 Shield Status') && comment.includes('Expected fail condition'));
    }

    testWorkflowAnalysis() {
        console.log('⚙️ Testing Workflow Analysis...');
        
        // Test successful shield analysis
        const workflowContext = {
            workflow_name: 'Shield Demo',
            job_name: 'keeper-shield-test',
            step_name: 'Execute Keeper Guard Logic',
            conclusion: 'failure',
            logs: 'Guard tripwire activated - expected protective fail',
            repository: 'test/repo',
            actor: 'test-user'
        };
        
        const analysis = this.shieldSystem.analyzeWorkflowForShields(workflowContext);
        this.assert('Workflow Shield Analysis', analysis.hasShields === true && analysis.shields.length > 0);
        
        // Test no shield for successful job
        const successContext = { ...workflowContext, conclusion: 'success' };
        const successAnalysis = this.shieldSystem.analyzeWorkflowForShields(successContext);
        this.assert('No Shield for Success', successAnalysis.hasShields === false);
    }

    testShieldSummaryGeneration() {
        console.log('📝 Testing Shield Summary Generation...');
        
        const mockAnalysis = {
            hasShields: true,
            shields: [
                {
                    type: 'Keeper\'s Shield',
                    subLabel: 'Guarded on purpose',
                    job: 'keeper-shield-test',
                    step: 'Execute Guard Logic',
                    emoji: '🛡'
                }
            ],
            badges: ['Keeper\'s Shield']
        };
        
        const summary = this.shieldSystem.generateShieldSummary(mockAnalysis);
        
        this.assert('Shield Summary Generation', 
            summary.includes('🛡 Pass-by-Fail Shield Status') && 
            summary.includes('Keeper\'s Shield') &&
            summary.includes('Code Evolution Theory')
        );
    }

    assert(testName, condition) {
        if (condition) {
            console.log(`  ✅ ${testName}`);
            this.passed++;
        } else {
            console.log(`  ❌ ${testName}`);
            this.failed++;
        }
    }

    printResults() {
        console.log('');
        console.log('🧪 TEST RESULTS');
        console.log('===============');
        console.log(`✅ Passed: ${this.passed}`);
        console.log(`❌ Failed: ${this.failed}`);
        console.log(`📊 Total: ${this.passed + this.failed}`);
        console.log('');
        
        if (this.failed === 0) {
            console.log('🛡 ALL TESTS PASSED - Shield system ready for deployment!');
            console.log('🍑 All cheeks preserved through comprehensive testing!');
        } else {
            console.log('⚠️ Some tests failed - review shield detection logic');
        }
        
        console.log('');
        console.log('📜 "Testing is not about finding bugs; it\'s about preserving the cheeks"');
        console.log('   — Quality Assurance Wisdom, Vol. II');
    }
}

// CLI interface
if (require.main === module) {
    const tester = new ShieldIndicatorTests();
    tester.runAllTests();
    
    // Exit with appropriate code
    process.exit(tester.failed === 0 ? 0 : 1);
}

module.exports = ShieldIndicatorTests;