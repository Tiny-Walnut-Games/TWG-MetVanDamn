# Warbler Pack Core

Essential conversation templates for the Warbler NPC conversation system.

## Overview

This content pack provides fundamental conversation templates that form the backbone of most NPC interactions. It includes greetings, farewells, help responses, trade inquiries, and general conversation fallbacks suitable for a wide variety of NPCs and scenarios.

## Installation

```bash
npm install warbler-pack-core
```

## Usage

### Basic Usage with Warbler Engine

```typescript
import { Warbler } from 'warbler-core';
import corePackTemplates from 'warbler-pack-core';

const warbler = new Warbler();

// Register all core pack templates
warbler.registerTemplates(corePackTemplates.templates);

// Or register specific templates
warbler.registerTemplate(corePackTemplates.greetingFriendly);
warbler.registerTemplate(corePackTemplates.farewellFormal);
```

### Individual Template Imports

```typescript
import { greetingFriendly, helpGeneral } from 'warbler-pack-core';
import { Warbler } from 'warbler-core';

const warbler = new Warbler();
warbler.registerTemplate(greetingFriendly);
warbler.registerTemplate(helpGeneral);
```

### JSON Template Access

```typescript
// Access raw template data
import templateData from 'warbler-pack-core/templates';
console.log('Available templates:', templateData.templates.length);
```

## Template Categories

### Greetings

- **`greeting_friendly`**: Casual, warm greeting for friendly NPCs
- **`greeting_formal`**: Professional greeting for officials and merchants

### Farewells

- **`farewell_friendly`**: Warm goodbye with well-wishes  
- **`farewell_formal`**: Polite, professional farewell

### Help & Assistance

- **`help_general`**: General offer of assistance and local knowledge

### Commerce

- **`trade_inquiry_welcome`**: Welcoming response to trade requests

### Conversation

- **`general_conversation`**: Fallback for maintaining conversation flow
- **`unknown_response`**: Graceful handling of unclear input

## Template Structure

Each template includes:

- **Unique ID**: Stable identifier for template selection
- **Semantic Version**: For tracking template evolution  
- **Content**: Response text with slot placeholders (`{{slot_name}}`)
- **Required Slots**: Variables needed for template completion
- **Tags**: Keywords for intent matching and categorization
- **Length Limits**: Maximum character constraints for responses

### Common Slots

Most core pack templates use these standard slots:

- `user_name` (string): Name to address the user
- `location` (string): Current scene or area name
- `time_of_day` (string): Current time period (morning, afternoon, etc.)
- `npc_name` (string): Name of the speaking NPC
- `user_title` (string): Formal address for the user

## Versioning Policy

This content pack follows semantic versioning with content-specific conventions:

- **Major versions** introduce breaking changes to template contracts or slot requirements
- **Minor versions** add new templates while maintaining backward compatibility
- **Patch versions** contain content improvements, typo fixes, and minor enhancements

## Template Validation

All templates in this pack are validated for:

- ✅ Required field presence (id, version, content, etc.)
- ✅ Unique template IDs within the pack
- ✅ Content length limits (all templates ≤ 200 characters)
- ✅ Valid slot type definitions
- ✅ Consistent slot naming conventions

## Integration Examples

### Complete NPC Setup

```typescript
import { Warbler, WarblerContext } from 'warbler-core';
import corePackTemplates from 'warbler-pack-core';

// Initialize conversation system
const warbler = new Warbler();
warbler.registerTemplates(corePackTemplates.templates);

// Set up NPC context
const context: WarblerContext = {
  npcId: 'merchant_sara',
  sceneId: 'marketplace',
  previousUtterances: [],
  worldState: { 
    time_of_day: 'morning',
    weather: 'sunny'
  },
  conversationHistory: []
};

// Process player greeting
const result = warbler.processConversation(
  'Good morning!', 
  context,
  { 
    user_name: 'Traveler',
    location: 'Riverside Market' 
  }
);

console.log(result.utterance?.content);
// Output: "Hello there, Traveler! Welcome to Riverside Market. It's a beautiful morning today, isn't it?"
```

### Custom Slot Providers

```typescript
// Extend with custom slot resolution
const customSlots = {
  user_name: playerData.characterName,
  location: gameState.currentArea.displayName,
  npc_name: npcDatabase.getNpcName(context.npcId),
  time_of_day: gameTime.getCurrentPeriod()
};

const result = warbler.processConversation(userInput, context, customSlots);
```

## Pack Metadata

```typescript
import { packMetadata } from 'warbler-pack-core';

console.log(`Pack: ${packMetadata.name} v${packMetadata.version}`);
console.log(`Templates: ${packMetadata.templates.length}`);
console.log(`Description: ${packMetadata.description}`);
```

## Contributing

This pack is part of the Warbler ecosystem. When contributing new templates:

1. Follow the established naming conventions (`category_variant`)
2. Include comprehensive slot documentation
3. Test templates with the validation script
4. Ensure content is appropriate for general audiences
5. Maintain semantic versioning for changes

### Development Workflow

```bash
# Install dependencies
npm install

# Build TypeScript exports
npm run build

# Validate template JSON
npm run validate

# Test integration
npm run prepublishOnly
```

## License

MIT License - see LICENSE file for details.

## Related Packages

- [`warbler-core`](../warbler-core) - Core conversation engine
- [`warbler-pack-faction-politics`](../warbler-pack-faction-politics) - Political intrigue templates
- Additional content packs available in the Warbler ecosystem

## Template Reference

| Template ID | Intent Types | Description | Slots Required |
|-------------|--------------|-------------|----------------|
| `greeting_friendly` | greeting, casual | Warm welcome | user_name*, location*, time_of_day* |
| `greeting_formal` | greeting, formal | Professional greeting | npc_name, user_title*, npc_role*, location*, time_of_day* |
| `farewell_friendly` | farewell, casual | Friendly goodbye | user_name* |
| `farewell_formal` | farewell, formal | Polite farewell | user_title* |
| `help_general` | help_request | General assistance | user_name*, location* |
| `trade_inquiry_welcome` | trade_inquiry | Commerce welcome | item_types* |
| `general_conversation` | general | Conversation fallback | location*, location_type* |
| `unknown_response` | general, fallback | Unclear input handler | (none) |

*Optional slots that enhance the response when provided