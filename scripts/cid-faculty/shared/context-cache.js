#!/usr/bin/env node
/**
 * CID Faculty - Context Cache
 * 
 * Provides diff-aware context caching to reduce processing when 
 * repository changes are minimal.
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

class ContextCache {
    constructor(cacheDir = 'out/cid/cache') {
        this.cacheDir = cacheDir;
        this.ensureCacheDir();
        console.log(`💾 Context cache initialized: ${this.cacheDir}`);
    }

    ensureCacheDir() {
        if (!fs.existsSync(this.cacheDir)) {
            fs.mkdirSync(this.cacheDir, { recursive: true });
        }
    }

    /**
     * Generate cache key from context fingerprint
     */
    generateCacheKey(context) {
        // Create a fingerprint of the context that would affect faculty analysis
        const fingerprint = {
            timestamp: Math.floor(Date.now() / (1000 * 60 * 60)), // hourly granularity
            topology: {
                dirs: context.topology?.dirs || [],
                totalLOC: context.topology?.totalLOC || 0,
                languages: context.topology?.languages || {},
                workflows: context.topology?.workflows || []
            },
            configs: context.configs || {},
            // Use git hash or timestamp as change indicator
            lastCommit: this.getLastCommitHash()
        };

        const fingerprintStr = JSON.stringify(fingerprint, Object.keys(fingerprint).sort());
        return crypto.createHash('sha256').update(fingerprintStr).digest('hex').substring(0, 16);
    }

    /**
     * Get last commit hash for cache invalidation
     */
    getLastCommitHash() {
        try {
            const { execSync } = require('child_process');
            return execSync('git rev-parse HEAD', { 
                stdio: ['ignore', 'pipe', 'ignore'],
                encoding: 'utf8'
            }).trim().substring(0, 8);
        } catch {
            return Date.now().toString(); // fallback to timestamp
        }
    }

    /**
     * Get cached context if available and valid
     */
    getCachedContext(cacheKey) {
        const cacheFile = path.join(this.cacheDir, `context-${cacheKey}.json`);
        
        if (!fs.existsSync(cacheFile)) {
            console.log(`💾 Cache miss: ${cacheKey} (file not found)`);
            return null;
        }

        try {
            const cached = JSON.parse(fs.readFileSync(cacheFile, 'utf8'));
            
            // Check if cache is still valid (within last 4 hours)
            const age = Date.now() - cached.timestamp;
            const maxAge = this.maxAgeMs;
            
            if (age > maxAge) {
                console.log(`💾 Cache expired: ${cacheKey} (age: ${Math.round(age / (60 * 1000))}m)`);
                return null;
            }

            console.log(`💾 Cache hit: ${cacheKey} (age: ${Math.round(age / (60 * 1000))}m)`);
            return cached;
        } catch (error) {
            console.log(`💾 Cache error: ${cacheKey} - ${error.message}`);
            return null;
        }
    }

    /**
     * Store context in cache
     */
    setCachedContext(cacheKey, context, facultyResults = null) {
        const cacheFile = path.join(this.cacheDir, `context-${cacheKey}.json`);
        
        const cacheData = {
            cacheKey,
            timestamp: Date.now(),
            context,
            facultyResults,
            version: '1.0.0'
        };

        try {
            fs.writeFileSync(cacheFile, JSON.stringify(cacheData, null, 2));
            console.log(`💾 Cached context: ${cacheKey}`);
        } catch (error) {
            console.log(`💾 Cache write error: ${error.message}`);
        }
    }

    /**
     * Get cached faculty results
     */
    getCachedFacultyResults(cacheKey, role) {
        const cacheFile = path.join(this.cacheDir, `faculty-${role}-${cacheKey}.json`);
        
        if (!fs.existsSync(cacheFile)) {
            return null;
        }

        try {
            const cached = JSON.parse(fs.readFileSync(cacheFile, 'utf8'));
            
            // Check age
            const age = Date.now() - cached.timestamp;
            const maxAge = 2 * 60 * 60 * 1000; // 2 hours for faculty results
            
            if (age > maxAge) {
                console.log(`💾 Faculty cache expired: ${role}-${cacheKey}`);
                return null;
            }

            console.log(`💾 Faculty cache hit: ${role}-${cacheKey}`);
            return cached.results;
        } catch (error) {
            console.log(`💾 Faculty cache error: ${error.message}`);
            return null;
        }
    }

    /**
     * Store faculty results in cache
     */
    setCachedFacultyResults(cacheKey, role, results) {
        const cacheFile = path.join(this.cacheDir, `faculty-${role}-${cacheKey}.json`);
        
        const cacheData = {
            cacheKey,
            role,
            timestamp: Date.now(),
            results,
            version: '1.0.0'
        };

        try {
            fs.writeFileSync(cacheFile, JSON.stringify(cacheData, null, 2));
            console.log(`💾 Cached faculty results: ${role}-${cacheKey}`);
        } catch (error) {
            console.log(`💾 Faculty cache write error: ${error.message}`);
        }
    }

    /**
     * Calculate enhanced diff between current and cached context
     * Now with granular change tracking and performance metrics
     */
    calculateContextDiff(currentContext, cachedContext) {
        if (!cachedContext) {
            return { 
                hasChanges: true, 
                changeRatio: 1.0, 
                changes: ['no cache'],
                diffType: 'initial_state',
                performanceMetrics: {
                    diffTimeMs: 0,
                    compressionRatio: 0.0,
                    changeMagnitude: 1.0
                }
            };
        }

        const startTime = Date.now();
        const changes = [];
        const detailedChanges = []; // Granular change tracking
        let totalFields = 0;
        let changedFields = 0;
        let totalMagnitude = 0;

        const compareObjects = (current, cached, path = '') => {
            const currentKeys = Object.keys(current || {});
            const cachedKeys = Object.keys(cached || {});
            const allKeys = new Set([...currentKeys, ...cachedKeys]);

            allKeys.forEach(key => {
                totalFields++;
                const currentVal = current?.[key];
                const cachedVal = cached?.[key];
                const keyPath = path ? `${path}.${key}` : key;

                // Skip timestamp fields for performance
                if (key.includes('timestamp') || key.includes('last_updated')) {
                    return;
                }

                const currentStr = JSON.stringify(currentVal);
                const cachedStr = JSON.stringify(cachedVal);
                
                if (currentStr !== cachedStr) {
                    changedFields++;
                    changes.push(keyPath);
                    
                    // Calculate change magnitude
                    let magnitude = this.calculateChangeMagnitude(cachedVal, currentVal);
                    totalMagnitude += magnitude;
                    
                    // Detailed change tracking
                    const changeType = this.determineChangeType(cachedVal, currentVal);
                    detailedChanges.push({
                        path: keyPath,
                        type: changeType,
                        oldValue: cachedVal,
                        newValue: currentVal,
                        magnitude: magnitude,
                        timestamp: Date.now()
                    });
                }

                if (typeof currentVal === 'object' && currentVal !== null && 
                    typeof cachedVal === 'object' && cachedVal !== null) {
                    compareObjects(currentVal, cachedVal, keyPath);
                }
            });
        };

        compareObjects(currentContext, cachedContext?.context);

        const diffTimeMs = Date.now() - startTime;
        const changeRatio = totalFields > 0 ? changedFields / totalFields : 0;
        const avgMagnitude = changedFields > 0 ? totalMagnitude / changedFields : 0;
        
        // Calculate compression ratio (how much smaller the diff is vs full context)
        const fullContextSize = JSON.stringify(currentContext).length;
        const diffSize = JSON.stringify(detailedChanges).length;
        const compressionRatio = fullContextSize > 0 ? 1 - (diffSize / fullContextSize) : 0;

        return {
            hasChanges: changes.length > 0,
            changeRatio,
            changes: changes.slice(0, 10), // Limit to first 10 changes for compatibility
            detailedChanges: detailedChanges.slice(0, 50), // More detailed tracking
            totalChanges: changes.length,
            totalFields,
            diffType: this.categorizeDiffType(changeRatio, avgMagnitude),
            performanceMetrics: {
                diffTimeMs,
                compressionRatio,
                changeMagnitude: avgMagnitude,
                changedFields,
                totalFields
            }
        };
    }

    /**
     * Calculate magnitude of change between two values
     */
    calculateChangeMagnitude(oldVal, newVal) {
        if (oldVal === null && newVal !== null) return 1.0;
        if (oldVal !== null && newVal === null) return 1.0;
        
        // Numeric changes
        if (typeof oldVal === 'number' && typeof newVal === 'number') {
            if (oldVal === 0) return newVal !== 0 ? 1.0 : 0.0;
            const ratio = Math.abs(newVal - oldVal) / Math.abs(oldVal);
            return Math.min(ratio, 1.0);
        }
        
        // String changes
        if (typeof oldVal === 'string' && typeof newVal === 'string') {
            if (oldVal.length === 0 && newVal.length === 0) return 0.0;
            if (oldVal.length === 0 || newVal.length === 0) return 1.0;
            
            // Simple character-based similarity
            const maxLen = Math.max(oldVal.length, newVal.length);
            let commonChars = 0;
            for (let i = 0; i < Math.min(oldVal.length, newVal.length); i++) {
                if (oldVal[i] === newVal[i]) commonChars++;
            }
            return 1.0 - (commonChars / maxLen);
        }
        
        // Array changes
        if (Array.isArray(oldVal) && Array.isArray(newVal)) {
            const maxLen = Math.max(oldVal.length, newVal.length);
            if (maxLen === 0) return 0.0;
            const sizeDiff = Math.abs(oldVal.length - newVal.length);
            return Math.min(sizeDiff / maxLen, 1.0);
        }
        
        // Default for other types
        return oldVal !== newVal ? 0.5 : 0.0;
    }

    /**
     * Determine the type of change
     */
    determineChangeType(oldVal, newVal) {
        if (oldVal === null || oldVal === undefined) return 'added';
        if (newVal === null || newVal === undefined) return 'removed';
        if (typeof oldVal !== typeof newVal) return 'type_changed';
        if (Array.isArray(oldVal) && Array.isArray(newVal)) {
            if (oldVal.length !== newVal.length) return 'array_resized';
            return 'array_modified';
        }
        if (typeof oldVal === 'object') return 'object_modified';
        return 'value_changed';
    }

    /**
     * Categorize the overall diff type
     */
    categorizeDiffType(changeRatio, avgMagnitude) {
        if (changeRatio === 0) return 'no_changes';
        if (changeRatio < 0.1) return 'minimal_changes';
        if (changeRatio < 0.3) return 'moderate_changes';
        if (avgMagnitude > 0.7) return 'major_changes';
        return 'significant_changes';
    }

    /**
     * Clean old cache files
     */
    cleanup(maxAgeHours = 24) {
        const maxAge = maxAgeHours * 60 * 60 * 1000;
        const now = Date.now();

        if (!fs.existsSync(this.cacheDir)) {
            return;
        }

        const files = fs.readdirSync(this.cacheDir);
        let cleaned = 0;

        files.forEach(file => {
            const filePath = path.join(this.cacheDir, file);
            try {
                const stats = fs.statSync(filePath);
                if (now - stats.mtime.getTime() > maxAge) {
                    fs.unlinkSync(filePath);
                    cleaned++;
                }
            } catch (error) {
                // Ignore errors on cleanup
            }
        });

        if (cleaned > 0) {
            console.log(`💾 Cleaned ${cleaned} old cache files`);
        }
    }

    /**
     * Get cache statistics
     */
    getStats() {
        if (!fs.existsSync(this.cacheDir)) {
            return { files: 0, totalSize: 0 };
        }

        const files = fs.readdirSync(this.cacheDir);
        let totalSize = 0;

        files.forEach(file => {
            try {
                const stats = fs.statSync(path.join(this.cacheDir, file));
                totalSize += stats.size;
            } catch (error) {
                // Ignore errors
            }
        });

        return {
            files: files.length,
            totalSize: Math.round(totalSize / 1024), // KB
            dir: this.cacheDir
        };
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const command = args[0] || 'help';
    
    if (command === 'help') {
        console.log(`
CID Faculty Context Cache Commands:
  stats       - Show cache statistics
  cleanup     - Clean old cache files
  test        - Test cache functionality
        `);
    } else if (command === 'stats') {
        const cache = new ContextCache();
        const stats = cache.getStats();
        console.log(`Cache: ${stats.files} files, ${stats.totalSize}KB in ${stats.dir}`);
    } else if (command === 'cleanup') {
        const cache = new ContextCache();
        cache.cleanup();
    } else if (command === 'test') {
        const cache = new ContextCache();
        const testContext = { test: 'data', timestamp: Date.now() };
        const cacheKey = cache.generateCacheKey(testContext);
        
        console.log('Generated cache key:', cacheKey);
        
        cache.setCachedContext(cacheKey, testContext);
        const retrieved = cache.getCachedContext(cacheKey);
        console.log('Cache test successful:', !!retrieved);
        console.log('Stats:', cache.getStats());
    }
}

module.exports = { ContextCache };