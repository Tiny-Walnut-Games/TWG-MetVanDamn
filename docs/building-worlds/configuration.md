# ⚙️ Configuring Your MetVanDAMN Worlds
## *Control How Your Worlds Generate*

> **"Configuration is like being a world director - you decide the cast, the setting, the plot. Let's make your worlds tell exactly the story you want!"**

---

## 🎯 **What You'll Learn**
- How to control world size and complexity
- Setting seeds for reproducible worlds
- Adjusting district count and layout
- Fine-tuning generation parameters
- Using debug tools to see results

**Perfect for**: People who want predictable, customized worlds
**Time**: 15 minutes
**Skills**: Unity Inspector, basic math

---

## 🌍 **The Configuration Component**

### **Finding the Settings**
1. In your scene, find the `WorldGenerator` GameObject
2. Select it to see the **SmokeTestSceneSetup** component
3. This component controls all world generation!

**All settings are in the Inspector panel on the right.**

---

## 🎲 **Seed: Your World DNA**

### **What is a Seed?**
A seed is a number that determines how your world generates. Same seed = same world!

**Examples**:
- Seed `42`: Balanced world with good flow
- Seed `123`: Lots of secrets and hidden areas
- Seed `999`: Challenging layout with tough connections
- Seed `1`: Simple, straightforward world

### **Why Seeds Matter**
- **Reproducible**: Share seeds to show others the same world
- **Testing**: Use same seed to compare changes
- **Favorites**: Save seeds for worlds you love

**Try it**: Change the seed and click Play to see different worlds!

---

## 📏 **World Size: How Big is Your Canvas?**

### **World Size Setting**
- **X and Y values**: Width and height of your world
- **Units**: Unity world units (1 unit = ~1 meter)

**Recommended Sizes**:
- **Small**: (30, 30) - Quick testing, simple worlds
- **Medium**: (50, 50) - Balanced, good for demos
- **Large**: (100, 100) - Complex worlds, more districts
- **Epic**: (200, 200) - Massive worlds (needs powerful computer)

### **Performance Impact**
- Bigger worlds = more districts = slower generation
- Start small, scale up as needed
- Test on your target hardware!

---

## 🏛️ **District Count: How Many Regions?**

### **Target Sector Count**
This controls how many districts (big regions) generate.

**Examples**:
- **3 districts**: Simple, focused world
- **5 districts**: Balanced adventure
- **8+ districts**: Complex, epic exploration

### **What Districts Do**
Each district contains:
- Multiple rooms of different types
- Unique environmental themes
- Connection points to other districts
- Special challenges or treasures

**Tip**: More districts = more variety, but also more complexity!

---

## 🔧 **Advanced Generation Settings**

### **Biome Transition Radius**
Controls how biome effects blend between districts.

**Low values (5-10)**: Sharp transitions, distinct regions
**High values (20-30)**: Smooth blending, gradual changes

### **Debug Settings**
- **Enable Debug Visualization**: Shows world bounds and layout
- **Log Generation Steps**: Prints detailed console messages

**Keep these ON while learning!**

---

## 🎮 **Testing Your Configurations**

### **Quick Test Workflow**
1. Change settings in Inspector
2. Click Play
3. Press **M** to see world map
4. Check console for generation messages
5. Stop and adjust settings
6. Repeat!

### **What to Look For**
- **Green bounds**: Shows world size
- **Colored squares**: Districts
- **Lines**: Connections between areas
- **Console messages**: Generation progress

---

## 🎨 **Creating Consistent World Types**

### **Adventure World (Exploration Focus)**
```
World Size: (80, 80)
District Count: 6
Seed: 42
Biome Radius: 15
```
*Lots of areas to discover, secrets to find*

### **Challenge World (Combat Focus)**
```
World Size: (60, 60)
District Count: 4
Seed: 999
Biome Radius: 10
```
*Tight, intense combat arenas*

### **Puzzle World (Logic Focus)**
```
World Size: (70, 70)
District Count: 5
Seed: 123
Biome Radius: 20
```
*Complex connections, requires thinking*

---

## 🔍 **Debug Visualization Guide**

### **World Map (Press M)**
- **Yellow squares**: Hub/starting districts
- **Blue shapes**: Regular districts
- **Green highlights**: Explored areas
- **Red outlines**: Gated/locked areas
- **White lines**: District connections

### **Console Messages**
Look for these success indicators:
```
🚀 MetVanDAMN Smoke Test: Starting world generation...
Created X districts based on targetSectorCount
✅ MetVanDAMN Smoke Test: World setup complete
```

### **Common Issues**
**"No districts created"**: Check world size isn't too small
**"Generation failed"**: Look for error messages in console
**"World too empty"**: Increase district count or world size

---

## 🎯 **Configuration Best Practices**

### **Start Simple**
1. Use small world size (30x30)
2. Set district count to 3
3. Enable all debug options
4. Test and understand basic generation

### **Iterate Gradually**
1. Increase world size by 20 units at a time
2. Add 1-2 districts per iteration
3. Test performance on your hardware
4. Save working configurations!

### **Document Your Settings**
Keep notes on what settings create what results:
```
Seed 42, Size 50x50, Districts 5 = Perfect balance
Seed 123, Size 80x80, Districts 7 = Great secrets
```

---

## 🚀 **Advanced Configuration**

### **Multiple Worlds in One Project**
Create different scenes with different configurations:
- `World_Easy.unity` - Simple settings
- `World_Normal.unity` - Balanced settings
- `World_Hard.unity` - Complex settings

### **Runtime Configuration**
You can change settings while playing:
1. Stop the scene
2. Modify values in Inspector
3. Click Play again
4. See instant results!

### **Sharing Worlds**
Share your favorite configurations:
- Screenshot the Inspector settings
- Share the seed number
- Describe what makes it special

---

## 🎉 **Configuration Mastery Achieved!**

**You now know how to:**
- ✅ Control world generation with seeds
- ✅ Adjust size and complexity
- ✅ Balance district count and connections
- ✅ Use debug tools to understand results
- ✅ Create different world types

### **Next Level Challenges**

**Experiment Ideas**:
- Create a "speed run" world (small, direct paths)
- Design a "maze" world (many districts, complex connections)
- Build a "story" world (specific seed that tells a narrative)

**Advanced Techniques**:
- Combine settings for unique experiences
- Use configuration for different difficulty levels
- Create themed worlds (fire, ice, forest, etc.)

### **Remember**
Configuration is an art, not a science. Play with settings, observe results, and find what creates the worlds you love!

---

*"Every great game world started with someone tweaking settings and saying 'What if I try this?' Your creativity is the only limit!"*

**Ready to add gameplay?** Check out **[Districts & Rooms](districts-rooms.md)** to design your world's structure!

**🍑 ✨ Keep Configuring! ✨ 🍑**
