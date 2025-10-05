# 🚀 Getting Started with MetVanDAMN
## *Your First Steps to Procedural World Building*

> **"Starting with MetVanDAMN is like getting the keys to a world-building machine. Let's turn that key and see what amazing places we can create!"**

---

## 🎯 **What You'll Learn**
By the end of this guide, you'll have:
- ✅ MetVanDAMN installed and running
- ✅ Created your first procedural world
- ✅ Seen how world generation works
- ✅ Know how to explore and customize

**Time Estimate**: 15 minutes
**Difficulty**: Beginner Friendly
**What You Need**: Unity 6000.2+, Git, Internet

---

## 🛠️ **Step 1: Install What You Need (5 minutes)**

### **Required Software**
You'll need these three things - don't worry, they're all free!

#### **1. Unity 6000.2.0f1 or Newer**
Unity is the game engine that powers MetVanDAMN.

**Download**: Go to [unity.com/download](https://unity3d.com/get-unity/download) and get Unity 6000.2.0f1 or newer.

**Why this version?** MetVanDAMN uses Unity's newest features for super-fast world generation.

#### **2. Git**
Git helps you download and update projects.

**Windows**: Download from [git-scm.com](https://git-scm.com/downloads)
**Mac**: Install Xcode Command Line Tools with `xcode-select --install`
**Linux**: `sudo apt install git` (Ubuntu/Debian) or `sudo dnf install git` (Fedora)

#### **3. Visual Studio Code (Optional but Recommended)**
Great for reading and editing code.

**Download**: [code.visualstudio.com](https://code.visualstudio.com/)

---

## 📥 **Step 2: Get MetVanDAMN (2 minutes)**

### **Download the Project**
Open a terminal/command prompt and run:

```bash
# Copy this command and paste it in your terminal:
git clone https://github.com/jmeyer1980/TWG-MetVanDamn.git

# Then go into the project folder:
cd TWG-MetVanDamn
```

**What just happened?**
- `git clone` downloads the entire MetVanDAMN project
- You now have all the code, tools, and examples!

---

## 🎮 **Step 3: Open in Unity (3 minutes)**

### **Launch Unity Hub**
1. Open the Unity Hub application
2. If you don't have it, download from [unity.com/download](https://unity3d.com/get-unity/download)

### **Add the Project**
1. Click **"Add"** button in Unity Hub
2. Navigate to where you cloned MetVanDAMN
3. Select the `TWG-MetVanDamn` folder
4. Choose **Unity 6000.2.0f1** from the version dropdown
5. Click **"Open"**

### **First Unity Load**
Unity will:
- Import thousands of files (this takes 2-3 minutes)
- Compile scripts (shows progress in bottom right)
- Open the project window

**You're in!** Welcome to the MetVanDAMN editor.

---

## 🌍 **Step 4: Create Your First World (5 minutes)**

### **The Magic Button**
1. Look at the top menu bar in Unity
2. Go to **Tools > MetVanDAMN > Create Base DEMO Scene**
3. Choose **"Complete 2D Platformer Demo"**
4. Click **OK**

**What happens?** Unity creates a complete scene with:
- A procedural world that's already generated
- A player character you can control
- Enemies that move around intelligently
- Treasure chests and interactive objects

### **See Your World!**
1. Click the **Play** button (triangle at the top)
2. Use **Arrow Keys** to move around
3. Press **Space** to jump
4. Press **M** to see the full world map

**Amazing!** You're now exploring a completely unique world that was generated just for you.

---

## 🔍 **Step 5: Understand What You See**

### **The World Around You**
Your generated world contains:
- **Districts**: Big areas connected by paths (like towns or regions)
- **Rooms**: Smaller spaces within districts (like houses or caves)
- **Gates**: Doors or barriers that require abilities to pass
- **Biomes**: Environmental themes (forest, cave, fire, ice)

### **Debug Visualization**
Press **M** again to see the world map. You'll see:
- 🟡 **Yellow squares**: Districts (big areas)
- 🔵 **Blue shapes**: Rooms (smaller spaces)
- ⚪ **White lines**: Connections between areas
- 🌈 **Colored overlays**: Biome effects

### **Try Different Seeds**
1. Stop playing (click Play button again)
2. Find the `SmokeTestSceneSetup` component in the scene
3. Change the `worldSeed` number (try 12345)
4. Click Play again

**Different world!** Each seed creates a completely unique layout.

---

## 🎨 **Step 6: Customize Your World (Optional)**

### **Change World Size**
In the `SmokeTestSceneSetup` component:
- **worldSize**: Try (100, 100) for a bigger world
- **targetSectorCount**: Try 8 for more districts

### **Experiment with Biomes**
The world generates with different environmental effects:
- **Sun/Moon**: Day/night cycle effects
- **Heat/Cold**: Temperature-based challenges
- **Biome Fields**: Areas with special properties

---

## 🧪 **Step 7: Run the Validation Tests**

### **Check Everything Works**
1. Go to **Tools > MetVanDAMN > Compliance Validator**
2. Click **"Run Full Compliance Validation"**
3. Look for all ✅ green checkmarks

This ensures your setup is working perfectly!

---

## 🎉 **Congratulations! You're a World Builder Now!**

**What you accomplished:**
- ✅ Installed Unity and Git
- ✅ Downloaded and opened MetVanDAMN
- ✅ Created your first procedural world
- ✅ Explored a unique generated environment
- ✅ Learned about districts, rooms, and biomes
- ✅ Customized world generation settings

### **What's Next?**

**Ready for more?** Check out:
- **[World Basics](world-basics.md)** - Understand how MetVanDAMN thinks
- **[First World Tutorial](first-world.md)** - Build something from scratch
- **[Configuration Guide](../building-worlds/configuration.md)** - Control how worlds generate

**Want to show off?** Share screenshots of your generated worlds!

### **Common Questions**

**"Why does Unity take so long to load?"**
Unity is importing thousands of files and compiling code. This is normal for big projects!

**"My world looks different than expected?"**
World generation is random! Try different seeds or check the debug visualization.

**"Can I use this for 3D games?"**
Yes! MetVanDAMN works for 2D platformers, 3D exploration, and everything in between.

**"How do I save my world?"**
Generated worlds are procedural - they create fresh each time. For persistent worlds, check the advanced guides.

---

*"Every expert was once a beginner. Every world builder started with their first generated district. You're on an amazing journey - keep exploring!"*

**🍑 ✨ Happy World Building! ✨ 🍑**
