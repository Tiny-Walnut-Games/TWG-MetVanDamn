# DOTS Quad-Rig Humanoid Prototype

A complete Unity DOTS (Data-Oriented Technology Stack) implementation of a quad-based 2D character rig with billboard support for MetVanDAMN. This system enables reuse of humanoid animation workflows for 2D visuals without sprite renderer overhead.

## 🎯 Key Features

### ✅ **Quad Mesh System**
- Quad meshes UV-mapped to shared texture atlas
- Individual character parts (head, torso, arms, legs, hands, feet)
- Efficient GPU-friendly mesh generation
- Standard humanoid proportions with customization support

### ✅ **Bone Hierarchy Integration**
- Compatible with Unity's Animator system
- 20-bone standard humanoid hierarchy
- Proper parent-child bone relationships
- Bone name hash compatibility for animation curves

### ✅ **Y-Axis Billboard System**
- Characters always face the camera
- Smooth rotation transitions
- Y-axis constraint (no tilting)
- Configurable rotation speed

### ✅ **Biome Skin Swapping**
- Instant material/texture changes
- No rig or animation data alteration
- Multiple biome atlas support (Default, Forest, Desert, Ice, Volcanic)
- Maintains rendering performance

### ✅ **Alpha-Mask Silhouette Integrity**
- Clean character silhouettes
- No background color creep
- Consistent alpha cutoff values
- Proper depth testing

### ✅ **Complete Story Test**
- 6-phase validation system
- Every feature demonstrated
- No missing functionality
- Narrative coherence testing

## 🏗️ Architecture

### Core Components

1. **QuadRigHumanoid** - Main character component
2. **BillboardData** - Y-axis rotation control
3. **QuadMeshPart** - Individual body part configuration
4. **TextureAtlasData** - Biome skin management
5. **BoneHierarchyElement** - Bone structure definition

### Systems

1. **BillboardSystem** - Handles camera-facing rotation
2. **QuadMeshGenerationSystem** - Creates quad meshes
3. **BiomeSkinSwapSystem** - Manages material changes
4. **BoneHierarchySystem** - Updates bone transformations
5. **QuadRigRenderingSystem** - GPU skinning and batching

## 🚀 Quick Start

### 1. Add QuadRig Character to Scene

```csharp
// Create GameObject and add QuadRigHumanoidAuthoring component
var characterGO = new GameObject("QuadRigCharacter");
var authoring = characterGO.AddComponent<QuadRigHumanoidAuthoring>();

// Configure in inspector or via code
authoring.rigId = 1;
authoring.characterScale = 1.0f;
authoring.enableBillboard = true;
authoring.enableAlphaMask = true;
```

### 2. Run Story Test

```csharp
// Enable story test on authoring component
authoring.enableStoryTest = true;

// Or run manually
var storyTest = characterGO.AddComponent<QuadRigStoryTest>();
storyTest.RunStoryTestManually();
```

### 3. Custom Biome Skin Swapping

```csharp
// Get the biome skin swap system
var skinSwapSystem = World.DefaultGameObjectInjectionWorld
    .GetExistingSystemManaged<BiomeSkinSwapSystem>();

// Request skin swap to forest biome
skinSwapSystem.RequestSkinSwap(characterEntity, 1); // 1 = Forest atlas
```

## 📋 Story Test Phases

The complete story test validates all requirements through a narrative sequence:

1. **Setup** - All actors take their positions
2. **Billboard Test** - Characters face the audience (camera)
3. **Skin Swap Test** - Costume changes (biome transitions)
4. **Animation Test** - Characters come alive (bone movement)
5. **Alpha Mask Test** - Perfect silhouettes maintained
6. **Validation** - Every promise kept, no missing functionality

## 🔧 Customization

### Custom Character Parts

```csharp
// Define custom quad parts
var customParts = new QuadPartConfiguration[]
{
    new()
    {
        partType = QuadPartType.Head,
        quadSize = new Vector2(1.0f, 1.2f),
        localOffset = new Vector3(0, 1.8f, 0),
        uvRect = new Rect(0, 0.75f, 0.25f, 0.25f)
    }
    // ... additional parts
};
```

### Custom Biome Atlas

```csharp
// Create biome atlas configuration
var customAtlas = BiomeSkinUtility.CreateBiomeAtlas(
    atlasId: 100,
    biomeType: BiomeSkinUtility.BiomeType.Underground,
    dimensions: new int2(1024, 1024)
);
```

### Custom Bone Hierarchy

```csharp
// Create custom bone hierarchy
var customBones = new BoneHierarchyElement[]
{
    new(boneIndex: 0, parentIndex: -1, // Root bone
        localPosition: float3.zero,
        localRotation: quaternion.identity,
        boneNameHash: Animator.StringToHash("Root"))
    // ... additional bones
};
```

## 🧪 Testing

### Unit Tests

```bash
# Run QuadRig tests
Unity.exe -runTests -testPlatform PlayMode -testFilter QuadRigHumanoidTests
```

### Story Test Validation

```csharp
// Validate story test requirements
Assert.That(humanoid.EnableBillboard, Is.True, "Billboard must be enabled");
Assert.That(humanoid.EnableAlphaMask, Is.True, "Alpha mask required");
Assert.That(billboard.IsActive, Is.True, "Billboard system must be active");
```

## 📊 Performance

- **GPU Skinning**: Efficient bone matrix uploads
- **Batching**: Similar characters batched together
- **Memory**: Shared texture atlases reduce memory usage
- **Rendering**: No sprite renderer overhead

## 🎨 Biome Atlas Layout

Standard 4x4 atlas layout (512x512 recommended):

```
[Head  ][Torso ][L.Arm ][R.Arm ]
[L.Leg ][R.Leg ][L.Hand][R.Hand]
[L.Foot][R.Foot][      ][      ]
[      ][      ][      ][      ]
```

Each cell: 128x128 pixels (0.25 normalized UV space)

## ⚠️ Requirements

- Unity 2023.3+
- Unity Entities 1.2+
- Unity Burst 1.8+
- Unity Mathematics 1.3+
- Unity Rendering Hybrid 1.2+

## 🔍 Troubleshooting

### Billboard Not Working
- Ensure `BillboardData.IsActive` is true
- Check camera position is set on `BillboardSystem`
- Verify `QuadRigHumanoid.EnableBillboard` is enabled

### Skin Swap Not Applying
- Verify `TextureAtlasData.AtlasId` matches request
- Check material cache in `BiomeSkinSwapSystem`
- Ensure `RenderMesh` component exists

### Animation Issues
- Validate bone hierarchy with `BoneHierarchyUtility.ValidateBoneHierarchy()`
- Check bone name hashes match Unity Animator
- Verify parent-child relationships are correct

## 📄 License

This package is part of the MetVanDAMN project by Tiny Walnut Games.

## 🤝 Contributing

See the main MetVanDAMN repository for contribution guidelines.

---

**🎭 "Every actor delivers their lines perfectly - no missing beats, no unexplained states."**  
*The QuadRig Story Test Promise*