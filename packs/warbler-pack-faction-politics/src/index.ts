/**
 * Warbler Faction Politics Pack - Political intrigue conversation templates
 * 
 * Re-exports templates for dynamic loading in the Warbler conversation system
 */

import { WarblerTemplate, WarblerPackMetadata } from 'warbler-core';
import templatesData from '../pack/templates.json';

// Transform JSON data to proper WarblerTemplate objects
export const templates: WarblerTemplate[] = templatesData.templates.map(template => ({
  ...template,
  requiredSlots: template.requiredSlots.map(slot => ({
    name: slot.name,
    type: slot.type as 'string' | 'number' | 'boolean' | 'object',
    required: slot.required,
    description: slot.description
  }))
}));

export const packMetadata: WarblerPackMetadata = {
  name: templatesData.packInfo.name,
  version: templatesData.packInfo.version,
  description: templatesData.packInfo.description,
  author: templatesData.packInfo.author,
  templates
};

// Export individual templates for selective imports
export const warningPoliticalThreat = templates.find(t => t.id === 'warning_political_threat')!;
export const intrigueInformationTrade = templates.find(t => t.id === 'intrigue_information_trade')!;
export const allianceProposal = templates.find(t => t.id === 'alliance_proposal')!;
export const betrayalRevelation = templates.find(t => t.id === 'betrayal_revelation')!;
export const factionLoyaltyTest = templates.find(t => t.id === 'faction_loyalty_test')!;
export const diplomaticImmunityClaim = templates.find(t => t.id === 'diplomatic_immunity_claim')!;

// Default export for easy bulk import
export default {
  templates,
  packMetadata,
  warningPoliticalThreat,
  intrigueInformationTrade,
  allianceProposal,
  betrayalRevelation,
  factionLoyaltyTest,
  diplomaticImmunityClaim
};