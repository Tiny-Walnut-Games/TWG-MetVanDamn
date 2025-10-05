# 🏗️ Districts & Rooms: World Structure
## *Designing the Bones of Your World*

> **"Districts are like neighborhoods in a city - each has its own personality, shops, and secrets. Rooms are the buildings that make up those neighborhoods. Let's design an amazing city together!"**

---

## 🎯 **What You'll Learn**
- The difference between districts and rooms
- How they connect to create exploration
- Types of rooms and their purposes
- Designing logical progression
- Creating interesting layouts

**Perfect for**: People who want structured, meaningful worlds
**Time**: 15 minutes
**Skills**: Understanding game design, spatial reasoning

---

## 🏛️ **Districts: The Big Picture**

### **What Are Districts?**
Districts are large regions that give your world structure and personality.

**Think of them as**:
- Neighborhoods in a city
- Regions in a kingdom
- Areas in a dungeon
- Planets in a solar system

### **District Types**
Each district can have a different purpose:

**Hub Districts** (Starting Areas):
- Safe zones with shops and save points
- Tutorial areas for learning mechanics
- Central connection points

**Exploration Districts** (Adventure Areas):
- Filled with secrets and discoveries
- Side quests and optional content
- Scenic or beautiful areas

**Challenge Districts** (Combat Areas):
- Enemy-heavy zones
- Puzzle or skill-based challenges
- High-risk, high-reward areas

**Transition Districts** (Connector Areas):
- Bridges between major regions
- Travel hubs with fast travel
- Areas that teach new mechanics

---

## 🚪 **Rooms: The Building Blocks**

### **What Are Rooms?**
Rooms are smaller spaces within districts that serve specific functions.

**Types of Rooms**:

#### **Functional Rooms**
- **Entrance Rooms**: How players enter districts
- **Exit Rooms**: How players leave districts
- **Save Rooms**: Safe zones with checkpoints
- **Shop Rooms**: Places to buy items/upgrades

#### **Gameplay Rooms**
- **Combat Rooms**: Enemy encounters and battles
- **Puzzle Rooms**: Logic puzzles and challenges
- **Treasure Rooms**: Rewards and collectibles
- **Secret Rooms**: Hidden areas with special rewards

#### **Atmospheric Rooms**
- **View Rooms**: Scenic overlooks or beautiful vistas
- **Story Rooms**: Cutscenes or narrative moments
- **Transition Rooms**: Areas that change between districts

### **Room Connections**
Rooms connect through:
- **Doors**: Normal entrances/exits
- **One-way Drops**: Can fall down but not climb back
- **Secret Passages**: Hidden connections
- **Teleporters**: Instant transport between areas

---

## 🗺️ **World Layout Principles**

### **Logical Flow**
Good worlds guide players naturally:

**Beginning → Middle → End**:
1. **Tutorial District**: Learn basics
2. **Easy Districts**: Build confidence
3. **Medium Districts**: Main adventure
4. **Hard Districts**: Climax challenges
5. **Final District**: Epic conclusion

### **Branching Paths**
Create choice and replayability:

```
Start → District A → Boss → End
    ↘ District B → Secret → End
        ↘ District C → Hard Mode → End
```

### **Loop Design**
Let players return to old areas with new abilities:

**Classic MetroidVania Loop**:
1. Start with basic abilities
2. Explore as much as possible
3. Find new ability in hard-to-reach area
4. Return to previously inaccessible areas
5. Repeat with more powerful abilities

---

## 🎮 **Designing for Exploration**

### **Discovery Moments**
Make exploration rewarding:

**Visual Cues**:
- Distant landmarks that draw players
- Partially visible areas that tease secrets
- Environmental storytelling (broken bridges, locked doors)

**Audio Hints**:
- Distant sounds that suggest hidden areas
- Music changes that indicate important locations
- Ambient sounds that guide toward objectives

### **Progressive Unlocking**
Create satisfying ability growth:

**Early Game Abilities**:
- Double jump (reach higher areas)
- Wall jump (climb vertical surfaces)
- Basic attack (fight weak enemies)

**Mid Game Abilities**:
- Dash (cross gaps, break blocks)
- Bomb (destroy walls, find secrets)
- Grapple (swing across chasms)

**Late Game Abilities**:
- Fly/Glide (access sky areas)
- Powerful attacks (fight bosses)
- Transform (access new areas)

---

## 🧱 **Room Design Patterns**

### **Combat Room Patterns**

**Arena Style**:
```
[Enemies] [Enemies]
[Player] [Player]
[Enemies] [Enemies]
```
*Open space for fair fights*

**Corridor Style**:
```
Enemy → Space → Enemy → Space → Enemy
```
*Wave-based encounters*

**Ambush Style**:
```
         [Hidden Enemy]
[Player] → Trap → [Treasure]
         [Hidden Enemy]
```
*Surprise elements*

### **Puzzle Room Patterns**

**Switch Logic**:
```
[Switch A] → [Door 1]
[Switch B] → [Door 2]
[Both needed for Door 3]
```
*Teach boolean logic*

**Platforming**:
```
[Moving Platform] → [Gap] → [Moving Platform]
[Spiked Pit] → [Safe Platform] → [Exit]
```
*Skill-based challenges*

**Environmental**:
```
[Wind pushes left] → [Push block against wind]
[Water current] → [Swim against current]
```
*Use world physics*

---

## 🌊 **Biome Integration**

### **District Themes**
Make districts feel distinct:

**Forest District**:
- Tree-filled rooms
- Wildlife enemies
- Natural platforming
- Hidden groves and clearings

**Castle District**:
- Stone architecture
- Guard enemies
- Vertical towers
- Hidden passages in walls

**Cave District**:
- Dark atmosphere
- Mining enemies
- Collapsing floors
- Crystal formations

### **Biome Transitions**
Smooth changes between districts:

**Gradual Transition**:
- Forest edges become sparse
- Castle walls show weathering
- Cave entrances show natural rock

**Sharp Transition**:
- Magical portals between worlds
- Instant biome changes
- Dimensional rifts

---

## 🔗 **Connection Design**

### **Gate Types**
Control progression with smart barriers:

**Ability Gates**:
- Jump high enough? (height requirement)
- Strong enough? (break blocks)
- Fast enough? (run through wind)

**Story Gates**:
- Complete quest? (NPC requirements)
- Find item? (key hunting)
- Solve puzzle? (logic requirements)

**Environmental Gates**:
- Weather changes (time of day)
- Biome effects (temperature, pressure)
- World events (earthquakes, floods)

### **Secret Connections**
Reward curious players:

**Hidden Doors**:
- Illusion walls (walk through)
- Fake walls (bomb to destroy)
- Pressure plates (stand on to reveal)

**Alternate Paths**:
- Underground tunnels
- Rooftop routes
- Waterways and sewers

---

## 🎯 **Testing Your Design**

### **Playtesting Checklist**
- Can players reach all areas?
- Are there dead ends (frustrating)?
- Do abilities feel earned and useful?
- Is exploration rewarding?
- Can players return to old areas?

### **Debug Tools**
Use MetVanDAMN's visualization:
- Press **M** for world map
- Look for disconnected areas (red zones)
- Check console for navigation errors
- Test with different ability sets

---

## 🎨 **Design Examples**

### **Simple 3-District World**
```
Tutorial District → Combat District → Boss District
     ↓                    ↓
Shop Room          Treasure Room    Final Reward
Save Room          Secret Room      Credits
```

### **Complex 5-District World**
```
Hub District ──┬── Forest District ── Mountain District
               │         ↓
               ├── Cave District ─── Volcano District
               │         ↓
               └── Water District ── Ice District
```

### **Loop-Based World**
```
Start → Area 1 → Ability 1 → Area 2 → Ability 2 → Area 3
   ↑         ↓         ↑         ↓         ↑         ↓
   └─ Area 4 ← Ability 3 ← Area 5 ← Ability 4 ← Area 6 ←
```

---

## 🎉 **Structural Mastery Achieved!**

**You now understand:**
- ✅ Districts as world regions
- ✅ Rooms as functional spaces
- ✅ Connection design principles
- ✅ Progressive ability systems
- ✅ Exploration and discovery

### **Design Tips**
1. **Start simple** - 3 districts, clear progression
2. **Add loops** - Let players revisit areas
3. **Balance challenge** - Mix easy and hard areas
4. **Reward curiosity** - Hide secrets for explorers
5. **Test thoroughly** - Ensure all areas are reachable

### **Next Steps**
- **[Biomes](biomes.md)** - Add environmental themes
- **[Gates & Progression](gates-progression.md)** - Create ability-based locks
- **[Art Integration](../art-visuals/biome-art.md)** - Make your world beautiful

---

*"World design is like writing a story - every district, room, and connection should serve the narrative you're creating for your players."*

**🍑 ✨ Keep Designing! ✨ 🍑**
