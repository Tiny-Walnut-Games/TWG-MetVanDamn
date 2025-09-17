# Warbler Pack: Faction Politics

Specialized conversation templates for political intrigue, faction diplomacy, and court machinations in the Warbler NPC conversation system.

## Overview

This content pack provides sophisticated dialogue templates for NPCs involved in political intrigue, diplomatic negotiations, and factional conflicts. Perfect for games and narratives featuring court politics, espionage, alliances, and betrayals.

## Installation

```bash
npm install warbler-pack-faction-politics
```

## Usage

### Basic Usage with Warbler Engine

```typescript
import { Warbler } from 'warbler-core';
import politicsPackTemplates from 'warbler-pack-faction-politics';

const warbler = new Warbler();

// Register all politics pack templates
warbler.registerTemplates(politicsPackTemplates.templates);

// Or register specific templates
warbler.registerTemplate(politicsPackTemplates.warningPoliticalThreat);
warbler.registerTemplate(politicsPackTemplates.allianceProposal);
```

### Themed Template Sets

```typescript
import { 
  warningPoliticalThreat,
  intrigueInformationTrade,
  betrayalRevelation 
} from 'warbler-pack-faction-politics';

// Create a spy/informant NPC
const spyTemplates = [intrigueInformationTrade, betrayalRevelation];
warbler.registerTemplates(spyTemplates);

// Create a diplomatic NPC  
import { allianceProposal, diplomaticImmunityClaim } from 'warbler-pack-faction-politics';
const diplomatTemplates = [allianceProposal, diplomaticImmunityClaim];
warbler.registerTemplates(diplomatTemplates);
```

## Template Categories

### Threats & Warnings

- **`warning_political_threat`**: Veiled warnings about faction displeasure and consequences

### Information Trading

- **`intrigue_information_trade`**: Offering to trade political secrets and intelligence

### Diplomacy

- **`alliance_proposal`**: Diplomatic overtures for political cooperation
- **`diplomatic_immunity_claim`**: Claiming diplomatic protection and immunity

### Betrayal & Conspiracy

- **`betrayal_revelation`**: Revealing political betrayals and double-crosses
- **`faction_loyalty_test`**: Testing political allegiance and commitment

## Template Structure

### Political Slots

This pack introduces specialized slots for political scenarios:

- `faction_name` (string): Name of political faction
- `faction_leader` (string): Leader of the faction  
- `faction_pronoun` (string): Pronouns for faction leader
- `user_title` (string): Formal political title for the user
- `diplomatic_title` (string): Official diplomatic rank
- `target_faction` (string): Faction being discussed or targeted
- `rival_faction` (string): Opposing or enemy faction
- `betrayer_name` (string): Name of person committing betrayal
- `threat_description` (string): Description of common threat or enemy

### Common Usage Patterns

Most templates support contextual political conversations:

```typescript
const politicalContext = {
  npcId: 'court_advisor_001',
  sceneId: 'royal_court',
  worldState: {
    current_faction: 'House Starwind',
    rival_faction: 'House Blackmoor',
    political_tension: 'high'
  },
  conversationHistory: []
};

const politicalSlots = {
  faction_name: 'House Starwind',
  faction_leader: 'Lord Commander Theron',
  user_title: 'Honored Guest',
  location: 'the Royal Court'
};
```

## Advanced Examples

### Political Intrigue Scene

```typescript
import { Warbler, WarblerContext } from 'warbler-core';
import { warningPoliticalThreat, intrigueInformationTrade } from 'warbler-pack-faction-politics';

const warbler = new Warbler();
warbler.registerTemplate(warningPoliticalThreat);
warbler.registerTemplate(intrigueInformationTrade);

// Court advisor warns about faction consequences
const threatContext: WarblerContext = {
  npcId: 'advisor_suspicious',
  sceneId: 'private_chamber',
  previousUtterances: [],
  worldState: { 
    political_climate: 'tense',
    player_faction_standing: 'negative'
  },
  conversationHistory: []
};

const result = warbler.processIntent(
  { type: 'warning', confidence: 0.9, slots: {} },
  threatContext,
  {
    user_name: 'Sir Blackwood',
    faction_name: 'the Iron Circle',
    faction_leader: 'Magistrate Vex',
    faction_pronoun: 'them',
    location: 'the merchant district'
  }
);

console.log(result.utterance?.content);
// Output: "Sir Blackwood, I would tread carefully if I were you. The Iron Circle has long memories, and Magistrate Vex does not forget those who cross them. Your recent actions in the merchant district have not gone unnoticed."
```

### Diplomatic Negotiation

```typescript
import { allianceProposal, factionLoyaltyTest } from 'warbler-pack-faction-politics';

// Ambassador proposing alliance
const diplomaticSlots = {
  user_title: 'Your Lordship',
  our_faction: 'the Northern Alliance',
  threat_description: 'the growing shadow from the East'
};

const result = warbler.processIntent(
  { type: 'alliance', confidence: 0.85, slots: {} },
  context,
  diplomaticSlots
);

// Output: "The times ahead will test us all, Your Lordship. The Northern Alliance and your people share common interests against the growing shadow from the East. Perhaps it is time we discussed a more... formal arrangement between our houses?"
```

### Information Broker Scenario

```typescript
import { intrigueInformationTrade, betrayalRevelation } from 'warbler-pack-faction-politics';

// Spy offering information trade
const spySlots = {
  user_name: 'Captain',
  location: 'the Capital',
  target_faction: 'House Ravencrest'
};

const infoResult = warbler.processIntent(
  { type: 'intrigue', confidence: 0.9, slots: {} },
  context,
  spySlots
);

// Later revealing betrayal
const betrayalSlots = {
  user_name: 'Captain',
  betrayer_name: 'Lieutenant Hayes',
  betrayer_pronoun: 'He',
  rival_faction: 'the Shadow Syndicate',
  location: 'the harbor'
};

const betrayalResult = warbler.processIntent(
  { type: 'betrayal', confidence: 0.95, slots: {} },
  context,
  betrayalSlots
);
```

## Content Guidelines

This pack contains mature political themes suitable for:

- ✅ Political intrigue and court drama
- ✅ Diplomatic negotiations and alliance building
- ✅ Espionage and information trading
- ✅ Betrayal and conspiracy revelations
- ✅ Faction-based conflicts and loyalty tests

Content is designed for:
- Fantasy/medieval political settings
- Modern political thrillers
- Sci-fi diplomatic scenarios
- Any narrative requiring sophisticated political dialogue

## Template Reference

| Template ID | Intent Types | Primary Use | Key Slots |
|-------------|--------------|-------------|-----------|
| `warning_political_threat` | warning, politics | Faction warnings | faction_name*, faction_leader* |
| `intrigue_information_trade` | intrigue, trade | Information trading | target_faction* |
| `alliance_proposal` | alliance, diplomacy | Diplomatic overtures | our_faction*, threat_description* |
| `betrayal_revelation` | betrayal, revelation | Conspiracy reveals | betrayer_name*, rival_faction* |
| `faction_loyalty_test` | loyalty, test | Allegiance testing | faction_name*, faction_leader* |
| `diplomatic_immunity_claim` | diplomacy, immunity | Legal protection | npc_name*, faction_name* |

*Required slots for proper template function

## Versioning & Compatibility

- **Engine Compatibility**: Requires warbler-core ^0.1.0
- **Content Rating**: Mature political themes
- **Language**: Formal/elevated register appropriate for political discourse
- **Character Limits**: All templates ≤ 320 characters for reasonable response lengths

## Development & Contributing

This pack follows political dialogue conventions:

1. **Formal Register**: Uses elevated, courtly language
2. **Implicit Threats**: Suggests consequences without explicit violence
3. **Political Terminology**: Employs faction, diplomatic, and court language
4. **Contextual Awareness**: References political relationships and power structures

### Validation

```bash
npm run validate  # Validates template JSON structure
npm run build     # Compiles TypeScript exports
```

## License

MIT License - see LICENSE file for details.

## Related Packages

- [`warbler-core`](../warbler-core) - Core conversation engine
- [`warbler-pack-core`](../warbler-pack-core) - Essential conversation templates
- Additional specialized packs available in the Warbler ecosystem