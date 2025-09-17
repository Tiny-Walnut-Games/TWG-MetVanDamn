#!/usr/bin/env node
/**
 * Warbler Pack Validation Script
 * 
 * Validates template JSON files for structural correctness and content requirements
 */

import fs from 'fs';
import path from 'path';

class WarblerPackValidator {
  constructor() {
    this.errors = [];
    this.warnings = [];
  }

  /**
   * Validate a warbler pack template file
   */
  async validatePack(templatePath) {
    console.log(`🔍 Validating warbler pack: ${templatePath}`);
    
    try {
      if (!fs.existsSync(templatePath)) {
        this.addError(`Template file not found: ${templatePath}`);
        return false;
      }

      const content = fs.readFileSync(templatePath, 'utf8');
      const packData = JSON.parse(content);
      
      // Validate pack structure
      this.validatePackStructure(packData);
      
      // Validate each template
      if (packData.templates && Array.isArray(packData.templates)) {
        for (const template of packData.templates) {
          this.validateTemplate(template);
        }
      } else {
        this.addError('Templates array is missing or invalid');
      }
      
      // Report results
      this.reportResults();
      
      return this.errors.length === 0;
      
    } catch (error) {
      this.addError(`Failed to parse template file: ${error.message}`);
      this.reportResults();
      return false;
    }
  }

  validatePackStructure(packData) {
    const { packInfo } = packData;
    
    if (!packInfo) {
      this.addError('Pack info section is missing');
      return;
    }
    
    // Required pack info fields
    const requiredFields = ['name', 'version', 'description', 'author'];
    for (const field of requiredFields) {
      if (!packInfo[field]) {
        this.addError(`Pack info missing required field: ${field}`);
      }
    }
    
    // Validate version format
    if (packInfo.version && !this.isValidSemver(packInfo.version)) {
      this.addError(`Invalid semver format: ${packInfo.version}`);
    }
    
    // Validate compatible engine version
    if (packInfo.compatibleEngine && !this.isValidVersionRange(packInfo.compatibleEngine)) {
      this.addWarning(`Compatible engine version format may be invalid: ${packInfo.compatibleEngine}`);
    }
  }

  validateTemplate(template) {
    const templateId = template.id || '<unknown>';
    
    // Required template fields
    const requiredFields = ['id', 'version', 'title', 'description', 'content', 'requiredSlots', 'tags'];
    for (const field of requiredFields) {
      if (template[field] === undefined || template[field] === null) {
        this.addError(`Template '${templateId}' missing required field: ${field}`);
      }
    }
    
    // Validate template ID format
    if (template.id && !this.isValidTemplateId(template.id)) {
      this.addError(`Template ID '${template.id}' contains invalid characters`);
    }
    
    // Validate version
    if (template.version && !this.isValidSemver(template.version)) {
      this.addError(`Template '${templateId}' has invalid version: ${template.version}`);
    }
    
    // Validate content
    if (template.content) {
      this.validateTemplateContent(template, templateId);
    }
    
    // Validate required slots
    if (template.requiredSlots) {
      this.validateRequiredSlots(template.requiredSlots, templateId);
    }
    
    // Validate tags
    if (template.tags && Array.isArray(template.tags)) {
      if (template.tags.length === 0) {
        this.addWarning(`Template '${templateId}' has no tags`);
      }
    }
    
    // Validate max length
    if (template.maxLength && template.content && template.content.length > template.maxLength) {
      this.addError(`Template '${templateId}' content exceeds maxLength (${template.content.length} > ${template.maxLength})`);
    }
    
    // Check content length threshold (400 chars as specified)
    if (template.content && template.content.length > 400) {
      this.addWarning(`Template '${templateId}' content is quite long (${template.content.length} chars)`);
    }
  }

  validateTemplateContent(template, templateId) {
    const { content } = template;
    
    if (typeof content !== 'string') {
      this.addError(`Template '${templateId}' content must be a string`);
      return;
    }
    
    if (content.trim().length === 0) {
      this.addError(`Template '${templateId}' content cannot be empty`);
      return;
    }
    
    // Find slot placeholders
    const slotPlaceholders = content.match(/\{\{([^}]+)\}\}/g) || [];
    const slotNames = slotPlaceholders.map(placeholder => 
      placeholder.replace(/\{\{|\}\}/g, '').trim()
    );
    
    // Check if all slots are defined in requiredSlots
    if (template.requiredSlots) {
      const definedSlots = template.requiredSlots.map(slot => slot.name);
      
      for (const slotName of slotNames) {
        if (!definedSlots.includes(slotName)) {
          this.addWarning(`Template '${templateId}' uses undefined slot: {{${slotName}}}`);
        }
      }
    }
  }

  validateRequiredSlots(requiredSlots, templateId) {
    if (!Array.isArray(requiredSlots)) {
      this.addError(`Template '${templateId}' requiredSlots must be an array`);
      return;
    }
    
    const slotNames = new Set();
    const validTypes = ['string', 'number', 'boolean', 'object'];
    
    for (const slot of requiredSlots) {
      if (!slot.name) {
        this.addError(`Template '${templateId}' has slot missing name`);
        continue;
      }
      
      // Check for duplicate slot names
      if (slotNames.has(slot.name)) {
        this.addError(`Template '${templateId}' has duplicate slot name: ${slot.name}`);
      }
      slotNames.add(slot.name);
      
      // Validate slot type
      if (!slot.type || !validTypes.includes(slot.type)) {
        this.addError(`Template '${templateId}' slot '${slot.name}' has invalid type: ${slot.type}`);
      }
      
      // Validate required field
      if (typeof slot.required !== 'boolean') {
        this.addError(`Template '${templateId}' slot '${slot.name}' required field must be boolean`);
      }
    }
  }

  isValidTemplateId(id) {
    // Allow alphanumeric, underscores, hyphens
    return /^[a-zA-Z0-9_-]+$/.test(id);
  }

  isValidSemver(version) {
    // Basic semver validation
    return /^\d+\.\d+\.\d+$/.test(version);
  }

  isValidVersionRange(range) {
    // Basic version range validation (supports ^, ~, >=, etc.)
    return /^[\^~>=<\s]*\d+\.\d+\.\d+/.test(range);
  }

  addError(message) {
    this.errors.push(message);
  }

  addWarning(message) {
    this.warnings.push(message);
  }

  reportResults() {
    console.log('\n📊 Validation Results:');
    
    if (this.errors.length > 0) {
      console.log(`\n❌ Errors (${this.errors.length}):`);
      this.errors.forEach(error => console.log(`  • ${error}`));
    }
    
    if (this.warnings.length > 0) {
      console.log(`\n⚠️  Warnings (${this.warnings.length}):`);
      this.warnings.forEach(warning => console.log(`  • ${warning}`));
    }
    
    if (this.errors.length === 0 && this.warnings.length === 0) {
      console.log('✅ All validations passed!');
    }
    
    const status = this.errors.length === 0 ? 'PASS' : 'FAIL';
    console.log(`\n🎯 Final Status: ${status}`);
  }
}

// CLI Interface
async function main() {
  const args = process.argv.slice(2);
  
  if (args.length === 0) {
    console.log('Usage: validate-warbler-pack.mjs <template-file>');
    console.log('Example: validate-warbler-pack.mjs pack/templates.json');
    process.exit(1);
  }
  
  const templatePath = args[0];
  const validator = new WarblerPackValidator();
  
  const isValid = await validator.validatePack(templatePath);
  
  process.exit(isValid ? 0 : 1);
}

// Run if called directly
if (import.meta.url === `file://${process.argv[1]}`) {
  main().catch(error => {
    console.error('❌ Validation failed:', error.message);
    process.exit(1);
  });
}

export default WarblerPackValidator;