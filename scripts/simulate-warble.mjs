#!/usr/bin/env node
/**
 * Warbler Simulation Script
 * 
 * Runs a small simulation using warbler-core to ensure runtime sanity
 */

import fs from 'fs';
import path from 'path';

// Dynamic import for ES modules
async function loadWarbler() {
  try {
    // Try to load from the built packages
    const { Warbler } = await import('../packages/warbler-core/dist/index.js');
    return { Warbler };
  } catch (error) {
    console.error('❌ Failed to load warbler-core. Make sure it\'s built first.');
    console.error('Run: cd packages/warbler-core && npm run build');
    throw error;
  }
}

async function loadPackTemplates(packPath) {
  try {
    const templatePath = path.join(packPath, 'pack/templates.json');
    if (!fs.existsSync(templatePath)) {
      throw new Error(`Template file not found: ${templatePath}`);
    }
    
    const content = fs.readFileSync(templatePath, 'utf8');
    const packData = JSON.parse(content);
    
    // Convert to WarblerTemplate format
    return packData.templates.map(template => ({
      ...template,
      requiredSlots: template.requiredSlots.map(slot => ({
        name: slot.name,
        type: slot.type,
        required: slot.required,
        description: slot.description
      }))
    }));
  } catch (error) {
    console.error(`❌ Failed to load pack templates from ${packPath}:`, error.message);
    throw error;
  }
}

class WarblerSimulation {
  constructor() {
    this.events = [];
    this.tickCount = 0;
    this.maxTicks = 5;
  }

  async initialize() {
    console.log('🎭 Initializing Warbler simulation...');
    
    // Load warbler engine
    const { Warbler } = await loadWarbler();
    this.warbler = new Warbler();
    
    // Load content packs
    await this.loadContentPacks();
    
    // Set up simulation context
    this.setupSimulationContext();
    
    console.log(`✅ Simulation initialized with ${this.warbler.getTemplates().length} templates`);
  }

  async loadContentPacks() {
    const packPaths = [
      'packs/warbler-pack-core',
      'packs/warbler-pack-faction-politics'
    ];
    
    for (const packPath of packPaths) {
      try {
        const templates = await loadPackTemplates(packPath);
        this.warbler.registerTemplates(templates);
        console.log(`📦 Loaded ${templates.length} templates from ${packPath}`);
      } catch (error) {
        console.warn(`⚠️  Failed to load pack ${packPath}: ${error.message}`);
      }
    }
  }

  setupSimulationContext() {
    this.contexts = [
      {
        npcId: 'guard_001',
        sceneId: 'town_square',
        previousUtterances: [],
        worldState: { 
          time_of_day: 'morning',
          weather: 'sunny',
          political_tension: 'low'
        },
        conversationHistory: []
      },
      {
        npcId: 'merchant_002',
        sceneId: 'marketplace',
        previousUtterances: [],
        worldState: { 
          time_of_day: 'afternoon',
          market_busy: true,
          trade_season: 'active'
        },
        conversationHistory: []
      },
      {
        npcId: 'advisor_003',
        sceneId: 'royal_court',
        previousUtterances: [],
        worldState: { 
          time_of_day: 'evening',
          political_tension: 'high',
          court_session: 'active'
        },
        conversationHistory: []
      }
    ];
    
    this.userInputs = [
      'Hello there!',
      'Can you help me?',
      'I need to trade some items',
      'What do you know about the political situation?',
      'Goodbye!'
    ];
  }

  async runSimulation() {
    console.log(`\n🎯 Starting ${this.maxTicks}-tick simulation...\n`);
    
    for (let tick = 1; tick <= this.maxTicks; tick++) {
      this.tickCount = tick;
      console.log(`⏰ Tick ${tick}/${this.maxTicks}`);
      
      await this.simulateTick();
      
      // Add small delay for readability
      await this.sleep(100);
    }
    
    this.reportResults();
  }

  async simulateTick() {
    // Select random context and input for this tick
    const context = this.contexts[this.tickCount % this.contexts.length];
    const userInput = this.userInputs[this.tickCount % this.userInputs.length];
    
    console.log(`  👤 User to ${context.npcId}: "${userInput}"`);
    
    // Prepare slots based on context
    const slots = this.prepareSlots(context);
    
    try {
      // Process conversation
      const result = this.warbler.processConversation(userInput, context, slots);
      
      if (result.success && result.utterance) {
        const utterance = result.utterance;
        
        console.log(`  🎭 ${context.npcId}: "${utterance.content}"`);
        console.log(`     [Template: ${utterance.templateId}, Intent: ${utterance.intent.type}]`);
        
        // Record event
        this.events.push({
          tick: this.tickCount,
          type: 'npc_utterance',
          npcId: context.npcId,
          userInput,
          response: utterance.content,
          templateId: utterance.templateId,
          intentType: utterance.intent.type,
          confidence: utterance.intent.confidence,
          timestamp: Date.now()
        });
        
        // Update conversation history
        context.conversationHistory.push(utterance);
        
      } else {
        console.log(`  ❌ ${context.npcId}: [Failed to generate response: ${result.error}]`);
        
        // Record failure event
        this.events.push({
          tick: this.tickCount,
          type: 'npc_failure',
          npcId: context.npcId,
          userInput,
          error: result.error,
          timestamp: Date.now()
        });
      }
      
    } catch (error) {
      console.log(`  💥 ${context.npcId}: [Simulation error: ${error.message}]`);
      
      this.events.push({
        tick: this.tickCount,
        type: 'simulation_error',
        npcId: context.npcId,
        userInput,
        error: error.message,
        timestamp: Date.now()
      });
    }
    
    console.log('');
  }

  prepareSlots(context) {
    const baseSlots = {
      user_name: 'Traveler',
      location: this.getLocationName(context.sceneId),
      time_of_day: context.worldState.time_of_day || 'unknown',
      npc_name: this.getNpcName(context.npcId)
    };
    
    // Add context-specific slots
    if (context.sceneId === 'royal_court') {
      baseSlots.user_title = 'Honored Guest';
      baseSlots.faction_name = 'the Royal Court';
      baseSlots.faction_leader = 'His Majesty';
    } else if (context.sceneId === 'marketplace') {
      baseSlots.item_types = 'weapons, armor, and supplies';
    }
    
    return baseSlots;
  }

  getLocationName(sceneId) {
    const locationMap = {
      'town_square': 'the bustling town square',
      'marketplace': 'the busy marketplace',
      'royal_court': 'the royal court'
    };
    return locationMap[sceneId] || sceneId;
  }

  getNpcName(npcId) {
    const nameMap = {
      'guard_001': 'Captain Marcus',
      'merchant_002': 'Trader Elara',
      'advisor_003': 'Court Advisor Thane'
    };
    return nameMap[npcId] || npcId;
  }

  async sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  reportResults() {
    console.log('📊 Simulation Results Summary:\n');
    
    const successfulUtterances = this.events.filter(e => e.type === 'npc_utterance');
    const failures = this.events.filter(e => e.type === 'npc_failure');
    const errors = this.events.filter(e => e.type === 'simulation_error');
    
    console.log(`✅ Successful utterances: ${successfulUtterances.length}`);
    console.log(`❌ Failed responses: ${failures.length}`);
    console.log(`💥 Simulation errors: ${errors.length}`);
    console.log(`📈 Success rate: ${((successfulUtterances.length / this.events.length) * 100).toFixed(1)}%`);
    
    // Template usage statistics
    if (successfulUtterances.length > 0) {
      console.log('\n🎭 Template Usage:');
      const templateUsage = {};
      successfulUtterances.forEach(event => {
        templateUsage[event.templateId] = (templateUsage[event.templateId] || 0) + 1;
      });
      
      Object.entries(templateUsage)
        .sort(([,a], [,b]) => b - a)
        .forEach(([templateId, count]) => {
          console.log(`  • ${templateId}: ${count} uses`);
        });
    }
    
    // Intent distribution
    if (successfulUtterances.length > 0) {
      console.log('\n🎯 Intent Distribution:');
      const intentUsage = {};
      successfulUtterances.forEach(event => {
        intentUsage[event.intentType] = (intentUsage[event.intentType] || 0) + 1;
      });
      
      Object.entries(intentUsage)
        .sort(([,a], [,b]) => b - a)
        .forEach(([intentType, count]) => {
          console.log(`  • ${intentType}: ${count} occurrences`);
        });
    }
    
    // Output events as JSON for potential processing
    console.log('\n📋 Full Event Log (JSON):');
    console.log(JSON.stringify(this.events, null, 2));
    
    // Determine overall simulation success
    const hasUtterances = successfulUtterances.length > 0;
    const lowErrorRate = errors.length === 0;
    const overallSuccess = hasUtterances && lowErrorRate;
    
    console.log(`\n🎯 Overall Simulation: ${overallSuccess ? '✅ SUCCESS' : '❌ FAILED'}`);
    
    if (!overallSuccess) {
      if (!hasUtterances) {
        console.log('   Reason: No successful utterances generated');
      }
      if (!lowErrorRate) {
        console.log('   Reason: Simulation errors occurred');
      }
    }
    
    return overallSuccess;
  }
}

// CLI Interface
async function main() {
  console.log('🚀 Warbler Simulation Starting...\n');
  
  try {
    const simulation = new WarblerSimulation();
    await simulation.initialize();
    await simulation.runSimulation();
    
    // Exit with appropriate code
    const events = simulation.events;
    const hasSuccessfulEvents = events.some(e => e.type === 'npc_utterance');
    process.exit(hasSuccessfulEvents ? 0 : 1);
    
  } catch (error) {
    console.error('\n💥 Simulation failed:', error.message);
    console.error(error.stack);
    process.exit(1);
  }
}

// Run if called directly
if (import.meta.url === `file://${process.argv[1]}`) {
  main();
}