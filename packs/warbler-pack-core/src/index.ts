/**
 * Warbler Core Pack - Essential conversation templates
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
export const greetingFriendly = templates.find(t => t.id === 'greeting_friendly')!;
export const greetingFormal = templates.find(t => t.id === 'greeting_formal')!;
export const farewellFriendly = templates.find(t => t.id === 'farewell_friendly')!;
export const farewellFormal = templates.find(t => t.id === 'farewell_formal')!;
export const helpGeneral = templates.find(t => t.id === 'help_general')!;
export const tradeInquiryWelcome = templates.find(t => t.id === 'trade_inquiry_welcome')!;
export const generalConversation = templates.find(t => t.id === 'general_conversation')!;
export const unknownResponse = templates.find(t => t.id === 'unknown_response')!;

// Default export for easy bulk import
export default {
  templates,
  packMetadata,
  greetingFriendly,
  greetingFormal,
  farewellFriendly,
  farewellFormal,
  helpGeneral,
  tradeInquiryWelcome,
  generalConversation,
  unknownResponse
};