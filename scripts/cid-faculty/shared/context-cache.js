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
     * Calculate diff between current and cached context
     */
    calculateContextDiff(currentContext, cachedContext) {
        if (!cachedContext) {
            return { hasChanges: true, changeRatio: 1.0, changes: ['no cache'] };
        }

        const changes = [];
        let totalFields = 0;
        let changedFields = 0;

        const compareObjects = (current, cached, path = '') => {
            const currentKeys = Object.keys(current || {});
            const cachedKeys = Object.keys(cached || {});
            const allKeys = new Set([...currentKeys, ...cachedKeys]);

            allKeys.forEach(key => {
                totalFields++;
                const currentVal = current?.[key];
                const cachedVal = cached?.[key];
                const keyPath = path ? `${path}.${key}` : key;

                if (JSON.stringify(currentVal) !== JSON.stringify(cachedVal)) {
                    changedFields++;
                    changes.push(keyPath);
                }

                if (typeof currentVal === 'object' && currentVal !== null && 
                    typeof cachedVal === 'object' && cachedVal !== null) {
                    compareObjects(currentVal, cachedVal, keyPath);
                }
            });
        };

        compareObjects(currentContext, cachedContext?.context);

        const changeRatio = totalFields > 0 ? changedFields / totalFields : 0;

        return {
            hasChanges: changes.length > 0,
            changeRatio,
            changes: changes.slice(0, 10), // Limit to first 10 changes
            totalChanges: changes.length,
            totalFields
        };
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