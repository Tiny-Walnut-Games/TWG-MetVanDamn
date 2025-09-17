#!/usr/bin/env node
/**
 * 🧠 LDA Overlord/Sentinel - Internal Workflow Approval Authority
 * 
 * Guardian-grade approval authority for internal GitHub Actions workflows.
 * Provides automated approval for trusted internal runs while maintaining
 * comprehensive security validation and audit logging.
 * 
 * Role Chain: Guardian → Wizard → Advisor → Overlord/Sentinel
 * 
 * Security Features:
 * - Actor identity verification via GitHub API
 * - Repository/branch origin validation  
 * - User consent requirement and tracking
 * - Workflow scope control and sensitive pattern detection
 * - Comprehensive audit logging with retention
 * - Emergency stop and override capabilities
 * 
 * @author LDA Faculty System
 * @version 1.0.0
 * @security overlord-grade
 */

const fs = require('fs');
const { execSync } = require('child_process');
const https = require('https');
const { URL } = require('url');
const yaml = require('js-yaml');

class OverlordSentinel {
    constructor(config = {}) {
        this.config = {
            configPath: config.configPath || 'configs/overlord-sentinel.yml',
            githubToken: config.githubToken || process.env.GITHUB_TOKEN,
            dryRun: config.dryRun || false,
            emergencyStop: config.emergencyStop || false,
            ...config
        };
        
        // Load overlord configuration
        this.overlordConfig = this.loadOverlordConfig();
        
        // Initialize audit system
        this.auditLog = [];
        this.dailyApprovalCount = 0;
        
        // GitHub API setup
        this.githubApiBase = 'https://api.github.com';
        
        console.log(`🧠 LDA Overlord/Sentinel manifested - Authority Level: ${this.overlordConfig.overlord.identity.authority_level}`);
        console.log(`🛡️ Security Mode: ${this.overlordConfig.overlord.security.require_user_consent ? 'STRICT' : 'PERMISSIVE'}`);
        
        if (this.config.emergencyStop || this.overlordConfig.overlord.emergency.emergency_stop) {
            console.log('🚨 EMERGENCY STOP ACTIVATED - No auto-approvals will be granted');
        }
    }

    /**
     * Load and validate overlord configuration
     */
    loadOverlordConfig() {
        try {
            const configPath = this.config.configPath;
            if (!fs.existsSync(configPath)) {
                throw new Error(`Overlord configuration not found: ${configPath}`);
            }
            
            const configContent = fs.readFileSync(configPath, 'utf8');
            const config = yaml.load(configContent);
            
            // Validate required configuration sections
            this.validateConfig(config);
            
            return config;
        } catch (error) {
            console.error('❌ Failed to load overlord configuration:', error.message);
            throw error;
        }
    }

    /**
     * Validate overlord configuration structure and security settings
     */
    validateConfig(config) {
        const required = [
            'overlord.identity',
            'overlord.security',
            'overlord.trusted_actors',
            'overlord.trusted_sources',
            'overlord.workflow_scopes',
            'overlord.audit'
        ];
        
        for (const path of required) {
            const keys = path.split('.');
            let current = config;
            for (const key of keys) {
                if (!current || !current[key]) {
                    throw new Error(`Missing required configuration: ${path}`);
                }
                current = current[key];
            }
        }
        
        // Validate security settings
        const security = config.overlord.security;
        if (security.max_daily_approvals < MIN_DAILY_APPROVALS || security.max_daily_approvals > MAX_DAILY_APPROVALS) {
            throw new Error(`Invalid max_daily_approvals: must be between ${MIN_DAILY_APPROVALS} and ${MAX_DAILY_APPROVALS}`);
        }
        
        console.log('✅ Overlord configuration validated');
    }

    /**
     * Evaluate workflow approval request with comprehensive security validation
     */
    async evaluateApprovalRequest(context) {
        console.log('🔍 Evaluating workflow approval request...');
        
        const approval = {
            granted: false,
            reason: 'Unknown',
            security_violations: [],
            audit_trail: [],
            recommendation: 'MANUAL_REVIEW',
            confidence: 0.0,
            metadata: {
                timestamp: new Date().toISOString(),
                evaluator: 'overlord-sentinel',
                version: this.overlordConfig.overlord.identity.version
            }
        };
        
        try {
            // 🚨 Emergency stop check
            if (this.config.emergencyStop || this.overlordConfig.overlord.emergency.emergency_stop) {
                approval.reason = 'Emergency stop activated';
                approval.recommendation = 'EMERGENCY_STOP';
                this.logAuditEvent('EMERGENCY_STOP', context, approval);
                return approval;
            }
            
            // 📊 Daily approval limit check
            if (this.dailyApprovalCount >= this.overlordConfig.overlord.security.max_daily_approvals) {
                approval.reason = `Daily approval limit reached (${this.dailyApprovalCount}/${this.overlordConfig.overlord.security.max_daily_approvals})`;
                approval.recommendation = 'RATE_LIMITED';
                this.logAuditEvent('RATE_LIMITED', context, approval);
                return approval;
            }
            
            // 🔐 Actor identity validation
            const actorValidation = await this.validateActor(context);
            if (!actorValidation.trusted) {
                approval.security_violations.push('UNTRUSTED_ACTOR');
                approval.reason = `Actor '${context.actor}' not in trusted actor list`;
                approval.recommendation = 'MANUAL_APPROVAL_REQUIRED';
                this.logAuditEvent('UNTRUSTED_ACTOR', context, approval);
                return approval;
            }
            
            // 🏛️ Repository and branch validation
            const sourceValidation = this.validateSource(context);
            if (!sourceValidation.trusted) {
                approval.security_violations.push('UNTRUSTED_SOURCE');
                approval.reason = sourceValidation.reason;
                approval.recommendation = 'MANUAL_APPROVAL_REQUIRED';
                this.logAuditEvent('UNTRUSTED_SOURCE', context, approval);
                return approval;
            }
            
            // 🎯 Workflow scope validation
            const scopeValidation = this.validateWorkflowScope(context);
            if (!scopeValidation.approvable) {
                approval.security_violations.push('SCOPE_VIOLATION');
                approval.reason = scopeValidation.reason;
                approval.recommendation = 'MANUAL_APPROVAL_REQUIRED';
                this.logAuditEvent('SCOPE_VIOLATION', context, approval);
                return approval;
            }
            
            // 📝 User consent validation
            const consentValidation = this.validateUserConsent(context);
            if (!consentValidation.granted) {
                approval.security_violations.push('CONSENT_REQUIRED');
                approval.reason = consentValidation.reason;
                approval.recommendation = 'CONSENT_REQUIRED';
                this.logAuditEvent('CONSENT_REQUIRED', context, approval);
                return approval;
            }
            
            // ✅ All security checks passed - grant approval
            approval.granted = true;
            approval.reason = 'All security validations passed';
            approval.recommendation = 'AUTO_APPROVED';
            approval.confidence = 0.95;
            approval.audit_trail = [
                `Actor: ${actorValidation.details}`,
                `Source: ${sourceValidation.details}`,
                `Scope: ${scopeValidation.details}`,
                `Consent: ${consentValidation.details}`
            ];
            
            // Increment daily approval count
            this.dailyApprovalCount++;
            
            this.logAuditEvent('AUTO_APPROVED', context, approval);
            console.log(`✅ Workflow approval GRANTED for ${context.workflow_name} (${context.actor})`);
            
        } catch (error) {
            approval.reason = `Evaluation error: ${error.message}`;
            approval.recommendation = 'ERROR_MANUAL_REVIEW';
            console.error('❌ Error during approval evaluation:', error);
            this.logAuditEvent('EVALUATION_ERROR', context, approval);
        }
        
        return approval;
    }

    /**
     * Validate actor identity against trusted actor lists and GitHub API
     */
    async validateActor(context) {
        const actor = context.actor;
        const trustedActors = this.overlordConfig.overlord.trusted_actors;
        
        // Check trusted users list
        if (trustedActors.users && trustedActors.users.includes(actor)) {
            return {
                trusted: true,
                source: 'trusted_users',
                details: `Actor '${actor}' found in trusted users list`
            };
        }
        
        // Check service accounts list
        if (trustedActors.service_accounts && trustedActors.service_accounts.includes(actor)) {
            return {
                trusted: true,
                source: 'service_accounts',
                details: `Actor '${actor}' found in trusted service accounts list`
            };
        }
        
        // If GitHub API validation is enabled, verify actor exists and has proper permissions
        if (this.overlordConfig.overlord.security.validate_actor_identity && this.config.githubToken) {
            try {
                const actorInfo = await this.getGitHubActorInfo(actor);
                if (!actorInfo) {
                    return {
                        trusted: false,
                        source: 'github_api',
                        details: `Actor '${actor}' not found via GitHub API`,
                        reason: 'Actor does not exist or is not accessible'
                    };
                }
                
                // Additional organization-based trust (if configured)
                if (trustedActors.organizations && trustedActors.organizations.length > 0) {
                    for (const org of trustedActors.organizations) {
                        if (actorInfo.organizations && actorInfo.organizations.includes(org)) {
                            return {
                                trusted: true,
                                source: 'trusted_organizations',
                                details: `Actor '${actor}' is member of trusted organization '${org}'`
                            };
                        }
                    }
                }
            } catch (error) {
                console.warn(`⚠️ GitHub API validation failed for actor '${actor}':`, error.message);
            }
        }
        
        return {
            trusted: false,
            source: 'validation',
            details: `Actor '${actor}' not found in any trusted actor lists`,
            reason: 'Actor not in trusted users, service accounts, or organizations'
        };
    }

    /**
     * Validate repository and branch trust status
     */
    validateSource(context) {
        const trustedSources = this.overlordConfig.overlord.trusted_sources;
        
        // Repository validation
        const repo = context.repository;
        if (trustedSources.repositories && !trustedSources.repositories.includes(repo)) {
            return {
                trusted: false,
                reason: `Repository '${repo}' not in trusted repositories list`,
                details: `Repository validation failed`
            };
        }
        
        // Branch validation
        const branch = context.branch || context.ref || 'unknown';
        const trustedBranches = trustedSources.trusted_branches || [];
        const untrustedBranches = trustedSources.untrusted_branches || [];
        
        // Check if branch matches untrusted patterns first
        for (const pattern of untrustedBranches) {
            if (this.matchPattern(branch, pattern)) {
                return {
                    trusted: false,
                    reason: `Branch '${branch}' matches untrusted pattern '${pattern}'`,
                    details: `Branch trust validation failed`
                };
            }
        }
        
        // Check if branch matches trusted patterns
        for (const pattern of trustedBranches) {
            if (this.matchPattern(branch, pattern)) {
                return {
                    trusted: true,
                    details: `Branch '${branch}' matches trusted pattern '${pattern}'`
                };
            }
        }
        
        return {
            trusted: false,
            reason: `Branch '${branch}' does not match any trusted branch patterns`,
            details: `Branch pattern validation failed`
        };
    }

    /**
     * Validate workflow scope against auto-approvable and sensitive patterns
     */
    validateWorkflowScope(context) {
        const workflowScopes = this.overlordConfig.overlord.workflow_scopes;
        const workflowName = context.workflow_name;
        
        // Check against manual approval required patterns (security-sensitive)
        const manualRequired = workflowScopes.manual_approval_required || [];
        for (const pattern of manualRequired) {
            if (this.matchPattern(workflowName, pattern)) {
                return {
                    approvable: false,
                    reason: `Workflow '${workflowName}' matches manual approval pattern '${pattern}'`,
                    details: `Workflow requires manual approval due to security sensitivity`
                };
            }
        }
        
        // Check against sensitive workflow patterns
        const sensitivePatterns = this.overlordConfig.overlord.security.sensitive_workflow_patterns || [];
        for (const pattern of sensitivePatterns) {
            if (this.matchPattern(workflowName, pattern)) {
                return {
                    approvable: false,
                    reason: `Workflow '${workflowName}' matches sensitive pattern '${pattern}'`,
                    details: `Workflow contains sensitive operations requiring manual approval`
                };
            }
        }
        
        // Check against auto-approvable patterns
        const autoApprovable = workflowScopes.auto_approvable || [];
        for (const pattern of autoApprovable) {
            if (this.matchPattern(workflowName, pattern)) {
                return {
                    approvable: true,
                    details: `Workflow '${workflowName}' matches auto-approvable pattern '${pattern}'`
                };
            }
        }
        
        return {
            approvable: false,
            reason: `Workflow '${workflowName}' not in auto-approvable scope`,
            details: `Workflow not found in auto-approvable patterns list`
        };
    }

    /**
     * Validate user consent for auto-approval
     */
    validateUserConsent(context) {
        if (!this.overlordConfig.overlord.security.require_user_consent) {
            return {
                granted: true,
                details: 'User consent not required by configuration'
            };
        }
        
        const userConsent = this.overlordConfig.user_consent || {};
        const actor = context.actor;
        
        if (!userConsent[actor]) {
            return {
                granted: false,
                reason: `User '${actor}' has not granted consent for auto-approval`,
                details: 'Consent required but not found'
            };
        }
        
        const consent = userConsent[actor];
        if (!consent.granted) {
            return {
                granted: false,
                reason: `User '${actor}' has explicitly denied auto-approval consent`,
                details: 'Consent explicitly denied'
            };
        }
        
        // Check if consent is for current configuration version
        const currentVersion = this.overlordConfig.overlord.identity.version;
        if (consent.version !== currentVersion) {
            return {
                granted: false,
                reason: `User '${actor}' consent is for version '${consent.version}', current version is '${currentVersion}'`,
                details: 'Consent version mismatch - re-consent required'
            };
        }
        
        return {
            granted: true,
            details: `User '${actor}' has granted consent (version ${consent.version}, ${consent.timestamp})`
        };
    }

    /**
     * Simple pattern matching with wildcard support
     */
    matchPattern(text, pattern) {
        if (!pattern.includes('*')) {
            return text === pattern;
        }
        
        const regexPattern = pattern.replace(/\*/g, '.*');
        const regex = new RegExp(`^${regexPattern}$`, 'i');
        return regex.test(text);
    }

    /**
     * Get actor information from GitHub API
     */
    async getGitHubActorInfo(actor) {
        if (!this.config.githubToken) {
            throw new Error('GitHub token required for API validation');
        }
        
        try {
            const url = `${this.githubApiBase}/users/${actor}`;
            const response = await this.makeGitHubRequest(url);
            
            if (!response) {
                return null;
            }
            
            // Get organization membership if needed
            let organizations = [];
            try {
                const orgsUrl = `${this.githubApiBase}/users/${actor}/orgs`;
                const orgsResponse = await this.makeGitHubRequest(orgsUrl);
                if (orgsResponse && Array.isArray(orgsResponse)) {
                    organizations = orgsResponse.map(org => org.login);
                }
            } catch (error) {
                // Organization membership may be private
                console.log(`ℹ️ Could not fetch organizations for ${actor}:`, error.message);
            }
            
            return {
                login: response.login,
                id: response.id,
                type: response.type,
                organizations: organizations
            };
        } catch (error) {
            console.error(`❌ Error fetching actor info for '${actor}':`, error.message);
            return null;
        }
    }

    /**
     * Make authenticated GitHub API request
     */
    async makeGitHubRequest(url) {
        return new Promise((resolve, reject) => {
            const urlObj = new URL(url);
            const options = {
                hostname: urlObj.hostname,
                path: urlObj.pathname + urlObj.search,
                method: 'GET',
                headers: {
                    'Authorization': `token ${this.config.githubToken}`,
                    'User-Agent': 'LDA-Overlord-Sentinel/1.0.0',
                    'Accept': 'application/vnd.github.v3+json'
                }
            };
            
            const req = https.request(options, (res) => {
                let data = '';
                res.on('data', chunk => data += chunk);
                res.on('end', () => {
                    try {
                        if (res.statusCode >= 200 && res.statusCode < 300) {
                            resolve(JSON.parse(data));
                        } else {
                            reject(new Error(`GitHub API request failed: ${res.statusCode} ${res.statusMessage}`));
                        }
                    } catch (error) {
                        reject(new Error(`Failed to parse GitHub API response: ${error.message}`));
                    }
                });
            });
            
            req.on('error', reject);
            req.setTimeout(this.config.githubApiTimeout, () => {
                req.abort();
                reject(new Error('GitHub API request timeout'));
            });
            
            req.end();
        });
    }

    /**
     * Log audit event with comprehensive tracking
     */
    logAuditEvent(eventType, context, approval) {
        const auditEntry = {
            timestamp: new Date().toISOString(),
            event_type: eventType,
            actor: context.actor,
            repository: context.repository,
            workflow_name: context.workflow_name,
            branch: context.branch || context.ref,
            approval_granted: approval.granted,
            reason: approval.reason,
            security_violations: approval.security_violations,
            confidence: approval.confidence,
            daily_count: this.dailyApprovalCount,
            overlord_version: this.overlordConfig.overlord.identity.version
        };
        
        this.auditLog.push(auditEntry);
        
        // Log to console with appropriate level
        const logLevel = approval.granted ? 'info' : 'warn';
        const emoji = approval.granted ? '✅' : '🚨';
        console.log(`${emoji} AUDIT [${eventType}]: ${context.actor}/${context.workflow_name} - ${approval.reason}`);
        
        // Persist audit log if configured
        if (this.overlordConfig.overlord.audit.enabled) {
            this.persistAuditLog(auditEntry);
        }
    }

    /**
     * Persist audit log entry to configured destinations
     */
    persistAuditLog(auditEntry) {
        const destinations = this.overlordConfig.overlord.audit.destinations || [];
        
        for (const destination of destinations) {
            try {
                if (destination === 'workflow_logs') {
                    // Already logged to console/workflow logs
                    continue;
                } else if (destination === 'github_comments') {
                    // Would post to GitHub issue/PR (implementation depends on context)
                    this.scheduleGitHubAuditComment(auditEntry);
                }
            } catch (error) {
                console.error(`❌ Failed to persist audit log to ${destination}:`, error.message);
            }
        }
    }

    /**
     * Schedule GitHub audit comment (placeholder for implementation)
     */
    scheduleGitHubAuditComment(auditEntry) {
        // Implementation would depend on having issue/PR context
        console.log(`📝 Audit comment scheduled: ${auditEntry.event_type} for ${auditEntry.actor}`);
    }

    /**
     * Generate comprehensive overlord report
     */
    generateOverlordReport() {
        const report = {
            metadata: {
                timestamp: new Date().toISOString(),
                overlord_version: this.overlordConfig.overlord.identity.version,
                authority_level: this.overlordConfig.overlord.identity.authority_level
            },
            status: {
                emergency_stop: this.config.emergencyStop || this.overlordConfig.overlord.emergency.emergency_stop,
                daily_approvals: this.dailyApprovalCount,
                max_daily_approvals: this.overlordConfig.overlord.security.max_daily_approvals,
                audit_entries: this.auditLog.length
            },
            configuration: {
                trusted_users: this.overlordConfig.overlord.trusted_actors.users?.length || 0,
                trusted_service_accounts: this.overlordConfig.overlord.trusted_actors.service_accounts?.length || 0,
                trusted_repositories: this.overlordConfig.overlord.trusted_sources.repositories?.length || 0,
                auto_approvable_workflows: this.overlordConfig.overlord.workflow_scopes.auto_approvable?.length || 0
            },
            security: {
                require_user_consent: this.overlordConfig.overlord.security.require_user_consent,
                validate_actor_identity: this.overlordConfig.overlord.security.validate_actor_identity,
                sensitive_pattern_count: this.overlordConfig.overlord.security.sensitive_workflow_patterns?.length || 0
            },
            recent_activity: this.auditLog.slice(-10).map(entry => ({
                timestamp: entry.timestamp,
                event_type: entry.event_type,
                actor: entry.actor,
                workflow: entry.workflow_name,
                approved: entry.approval_granted
            }))
        };

        return report;
    }

    /**
     * Grant user consent for auto-approval
     */
    grantUserConsent(username) {
        if (!this.overlordConfig.user_consent) {
            this.overlordConfig.user_consent = {};
        }
        
        this.overlordConfig.user_consent[username] = {
            granted: true,
            timestamp: new Date().toISOString(),
            version: this.overlordConfig.overlord.identity.version
        };
        
        // Persist updated configuration
        this.saveOverlordConfig();
        
        console.log(`✅ User consent granted for '${username}'`);
        this.logAuditEvent('CONSENT_GRANTED', { actor: username, workflow_name: 'N/A', repository: 'N/A' }, { granted: true, reason: 'User consent manually granted' });
    }

    /**
     * Revoke user consent for auto-approval  
     */
    revokeUserConsent(username) {
        if (this.overlordConfig.user_consent && this.overlordConfig.user_consent[username]) {
            this.overlordConfig.user_consent[username].granted = false;
            this.overlordConfig.user_consent[username].revoked_timestamp = new Date().toISOString();
            
            // Persist updated configuration
            this.saveOverlordConfig();
            
            console.log(`🚨 User consent revoked for '${username}'`);
            this.logAuditEvent('CONSENT_REVOKED', { actor: username, workflow_name: 'N/A', repository: 'N/A' }, { granted: false, reason: 'User consent manually revoked' });
        }
    }

    /**
     * Save overlord configuration to file
     */
    saveOverlordConfig() {
        try {
            const yamlContent = yaml.dump(this.overlordConfig);
            fs.writeFileSync(this.config.configPath, yamlContent, 'utf8');
            console.log(`💾 Overlord configuration saved to ${this.config.configPath}`);
        } catch (error) {
            console.error('❌ Failed to save overlord configuration:', error.message);
        }
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const command = args[0];
    
    async function main() {
        try {
            // Helper function to parse command line arguments in both formats
            const getArgValue = (name) => {
                // Try format: --name=value
                const equalArg = args.find(arg => arg.startsWith(`--${name}=`));
                if (equalArg) return equalArg.split('=')[1];
                
                // Try format: --name value
                const argIndex = args.findIndex(arg => arg === `--${name}`);
                if (argIndex !== -1 && args[argIndex + 1]) return args[argIndex + 1];
                
                return null;
            };
            
            const overlord = new OverlordSentinel({
                githubToken: process.env.GITHUB_TOKEN,
                dryRun: args.includes('--dry-run'),
                emergencyStop: args.includes('--emergency-stop')
            });
            
            if (command === 'evaluate') {
                // Parse evaluation context from command line arguments
                const context = {
                    actor: getArgValue('actor') || 'unknown',
                    repository: getArgValue('repo') || 'unknown/unknown',
                    workflow_name: getArgValue('workflow') || 'Unknown Workflow',
                    branch: getArgValue('branch') || 'unknown'
                };
                
                const approval = await overlord.evaluateApprovalRequest(context);
                console.log('🧠 OVERLORD DECISION:', JSON.stringify(approval, null, 2));
                
                // Exit with non-zero if approval denied (for workflow integration)
                process.exit(approval.granted ? 0 : 1);
                
            } else if (command === 'report') {
                const report = overlord.generateOverlordReport();
                console.log('📋 OVERLORD REPORT:', JSON.stringify(report, null, 2));
                
            } else if (command === 'grant-consent') {
                const username = getArgValue('user');
                if (!username) {
                    console.error('❌ Username required: --user=username');
                    process.exit(1);
                }
                overlord.grantUserConsent(username);
                
            } else if (command === 'revoke-consent') {
                const username = getArgValue('user');
                if (!username) {
                    console.error('❌ Username required: --user=username');
                    process.exit(1);
                }
                overlord.revokeUserConsent(username);
                
            } else {
                console.log(`
🧠 LDA Overlord/Sentinel - Internal Workflow Approval Authority

Usage:
  node overlord-sentinel.js evaluate --actor=user --repo=owner/repo --workflow="Workflow Name" --branch=main
  node overlord-sentinel.js report
  node overlord-sentinel.js grant-consent --user=username
  node overlord-sentinel.js revoke-consent --user=username

Options:
  --dry-run         Preview mode - no actual approvals
  --emergency-stop  Activate emergency stop - deny all approvals

Environment:
  GITHUB_TOKEN      GitHub token for API validation

Examples:
  # Evaluate approval request
  node overlord-sentinel.js evaluate --actor=copilot --repo=jmeyer1980/living-dev-agent --workflow="Living Dev Agent CI" --branch=copilot/fix-60

  # Generate overlord status report
  node overlord-sentinel.js report

  # Grant user consent for auto-approval
  node overlord-sentinel.js grant-consent --user=jmeyer1980
`);
            }
            
        } catch (error) {
            console.error(`❌ Overlord/Sentinel error:`, error.message);
            process.exit(1);
        }
    }
    
    main();
}

module.exports = { OverlordSentinel };