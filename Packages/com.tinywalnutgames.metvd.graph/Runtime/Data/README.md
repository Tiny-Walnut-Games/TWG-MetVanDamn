# MetVanDAMN Sample WFC Data

This directory contains sample Wave Function Collapse tiles and socket definitions for testing the MetVanDAMN procedural generation engine.

## Tile Types

### Basic Tile Set
1. **Hub Tile (ID: 1)** - Central connection point
   - 4 open sockets, all directions
   - Neutral polarity
   - High connection capacity

2. **Corridor Tile (ID: 2)** - Linear connection
   - 2 open sockets, opposite directions  
   - Flexible polarity
   - Standard connection

3. **Chamber Tile (ID: 3)** - Room with multiple exits
   - 3 open sockets
   - Environment-specific polarity
   - Medium connection capacity

4. **Specialist Tile (ID: 4)** - Unique functionality
   - 1-2 open sockets
   - Strong polarity requirements
   - Low connection capacity

## Socket Compatibility

Sockets use ID-based matching with polarity constraints:
- Socket ID 1: Basic passages (any polarity)
- Socket ID 2: Environmental passages (polarity-restricted)
- Socket ID 3: Special passages (dual-polarity)

## Usage

These definitions are loaded by the WFC system during initialization to provide a working set of tiles for world generation testing.