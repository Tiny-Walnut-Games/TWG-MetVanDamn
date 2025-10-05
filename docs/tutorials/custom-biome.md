# 🎨 Custom Biome Tutorial: Create Your Own World Theme
## *Design Unique Environmental Experiences*

> **"What if your world had floating islands, crystal caves, or neon cities? Biomes let you create any atmosphere you can imagine. Let's build a magical crystal biome!"**

---

## 🎯 **What You'll Create**
A complete custom biome with:
- ✅ Unique visual theme (crystal aesthetics)
- ✅ Special environmental effects
- ✅ Custom gameplay mechanics
- ✅ Integrated art and audio
- ✅ Balanced difficulty curve

**Time**: 1-2 hours
**Difficulty**: Advanced Intermediate
**Skills**: Unity scripting, art integration, game design

---

## 🌟 **Biome Concept: Crystal Realm**

### **Theme Overview**
- **Aesthetic**: Glowing crystals, refracted light, geometric patterns
- **Atmosphere**: Mysterious, magical, reflective
- **Gameplay**: Light refraction puzzles, crystal growth mechanics
- **Challenge**: Slippery surfaces, light-based enemies

### **Why This Biome Works**
- Visually distinct from default biomes
- Introduces new mechanical concepts
- Creates memorable player experiences
- Can be balanced for different difficulty levels

---

## 🛠️ **Phase 1: Biome Setup (20 minutes)**

### **Step 1: Create Biome ScriptableObject**
1. Create folder: `Assets/ScriptableObjects/Biomes/`
2. Right-click: **Create > C# Script** > Name: `CrystalBiomeProfile`
3. Replace code with:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "CrystalBiome", menuName = "MetVanDAMN/Biome/Crystal Biome")]
public class CrystalBiomeProfile : ScriptableObject
{
    [Header("Visual Settings")]
    public Color crystalTint = new Color(0.7f, 0.9f, 1.0f, 0.8f);
    public float glowIntensity = 1.5f;
    public Material crystalMaterial;

    [Header("Environmental Effects")]
    public float slipperiness = 0.8f; // 0 = normal, 1 = very slippery
    public float lightRefraction = 0.3f; // How much light bends
    public bool enableCrystalGrowth = true;

    [Header("Gameplay Modifiers")]
    public float playerSpeedModifier = 0.9f; // Slightly slower
    public float jumpHeightModifier = 1.1f; // Slightly higher jumps
    public float enemyAggressionModifier = 1.2f; // More aggressive enemies

    [Header("Audio")]
    public AudioClip ambientSound;
    public AudioClip crystalBreakSound;
    public AudioClip lightRefractionSound;
}
```

### **Step 2: Create Biome Asset**
1. Right-click in Project: **Create > MetVanDAMN > Biome > Crystal Biome**
2. Name: `CrystalBiomeProfile`
3. Configure settings (we'll adjust these as we build)

### **Step 3: Create Crystal Material**
1. Right-click: **Create > Material**
2. Name: `CrystalMaterial`
3. Shader: **Standard**
4. Settings:
   - Albedo: Light blue tint
   - Metallic: `0.8`
   - Smoothness: `0.9`
   - Emission: Light blue glow

---

## 🎨 **Phase 2: Visual Effects (30 minutes)**

### **Step 1: Crystal Terrain Shader**
Create a custom shader for crystal surfaces:

```shader
Shader "Custom/CrystalSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _RefractionStrength ("Refraction Strength", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 viewDir;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _EmissionColor;
        half _RefractionStrength;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base texture
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            // Crystal refraction effect
            float fresnel = dot(IN.worldNormal, IN.viewDir);
            fresnel = 1.0 - saturate(fresnel);
            fresnel = pow(fresnel, 2.0);

            // Add refraction tint
            c.rgb += _EmissionColor.rgb * fresnel * _RefractionStrength;

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness + fresnel * 0.2;
            o.Emission = _EmissionColor.rgb * (0.5 + fresnel * 0.5);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
```

### **Step 2: Crystal Particle Effects**
Create particle system for crystal sparkles:

1. Right-click: **Create Empty** > Name: `CrystalParticles`
2. Add: **Particle System** component
3. Settings:
   - **Shape**: Sphere, Radius: `0.5`
   - **Emission**: Rate over Time: `20`
   - **Main**: Start Lifetime: `2-4`, Start Speed: `0.1-0.3`
   - **Renderer**: Material: Crystal material, Render Mode: Sprite

### **Step 3: Light Refraction Effect**
Create script for light bending:

```csharp
using UnityEngine;

public class CrystalRefraction : MonoBehaviour
{
    public float refractionStrength = 0.3f;
    public float effectRadius = 5f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Apply refraction effect to camera
            // This would require a post-processing effect
            // For simplicity, we'll use a simple distortion
            ApplyRefractionEffect();
        }
    }

    void ApplyRefractionEffect()
    {
        // Simple implementation - could be expanded with shaders
        if (mainCamera != null)
        {
            // Add lens distortion or chromatic aberration
            // This is a placeholder for the actual effect
        }
    }
}
```

---

## 🎮 **Phase 3: Gameplay Mechanics (40 minutes)**

### **Step 1: Slippery Movement Modifier**
Create script to modify player movement in crystal areas:

```csharp
using UnityEngine;

public class CrystalMovementModifier : MonoBehaviour
{
    public CrystalBiomeProfile biomeProfile;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyMovementModifiers(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RemoveMovementModifiers(other.gameObject);
        }
    }

    void ApplyMovementModifiers(GameObject player)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Make surfaces slippery
            rb.drag *= (1f - biomeProfile.slipperiness);
            rb.angularDrag *= (1f - biomeProfile.slipperiness);
        }

        // Modify player controller
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.moveSpeed *= biomeProfile.playerSpeedModifier;
            controller.jumpForce *= biomeProfile.jumpHeightModifier;
        }
    }

    void RemoveMovementModifiers(GameObject player)
    {
        // Reset to original values (would need to store originals)
        // This is simplified - in practice you'd store original values
    }
}
```

### **Step 2: Crystal Growth Mechanic**
Create interactive crystals that grow when activated:

```csharp
using UnityEngine;

public class CrystalGrowth : MonoBehaviour
{
    public float growthRate = 0.5f;
    public float maxSize = 3f;
    public bool isActivated = false;

    private Vector3 originalScale;
    private float growthProgress = 0f;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        if (isActivated && growthProgress < 1f)
        {
            growthProgress += growthRate * Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxSize, growthProgress);

            // Add particle effects during growth
            // Add sound effects
        }
    }

    public void ActivateGrowth()
    {
        if (!isActivated)
        {
            isActivated = true;
            // Play activation sound
            // Spawn particle effect
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && growthProgress >= 1f)
        {
            // Crystal is fully grown - maybe create platform or bridge
            CreateCrystalBridge();
        }
    }

    void CreateCrystalBridge()
    {
        // Instantiate bridge prefab or enable existing bridge
        // This could connect previously inaccessible areas
    }
}
```

### **Step 3: Light-Based Enemy**
Create enemies that react to light refraction:

```csharp
using UnityEngine;

public class LightRefractorEnemy : MonoBehaviour
{
    public float refractionRange = 3f;
    public float refractionStrength = 0.5f;
    public float moveSpeed = 2f;

    private Transform player;
    private bool isRefracting = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= refractionRange)
        {
            // Enter refraction mode
            isRefracting = true;
            RefractTowardPlayer();
        }
        else
        {
            isRefracting = false;
            // Normal movement
        }
    }

    void RefractTowardPlayer()
    {
        // Calculate refracted path to player
        Vector3 toPlayer = player.position - transform.position;
        Vector3 refractedDirection = Vector3.Reflect(toPlayer.normalized, Vector3.up) * refractionStrength;

        // Move in refracted direction
        transform.position += refractedDirection * moveSpeed * Time.deltaTime;

        // Visual effect - maybe change color or add trail
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, refractionRange);
    }
}
```

---

## 🔊 **Phase 4: Audio & Polish (20 minutes)**

### **Step 1: Crystal Audio System**
Create ambient audio for crystal areas:

```csharp
using UnityEngine;

public class CrystalAudioSystem : MonoBehaviour
{
    public CrystalBiomeProfile biomeProfile;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Setup ambient audio
        audioSource.clip = biomeProfile.ambientSound;
        audioSource.loop = true;
        audioSource.volume = 0.3f;
        audioSource.Play();
    }

    public void PlayCrystalBreak()
    {
        if (biomeProfile.crystalBreakSound != null)
        {
            audioSource.PlayOneShot(biomeProfile.crystalBreakSound);
        }
    }

    public void PlayRefractionEffect()
    {
        if (biomeProfile.lightRefractionSound != null)
        {
            audioSource.PlayOneShot(biomeProfile.lightRefractionSound, 0.5f);
        }
    }
}
```

### **Step 2: Visual Polish**
Add final visual touches:

1. **Crystal Shards**: Small floating crystal pieces
2. **Light Rays**: Volumetric light effects
3. **Refraction Trails**: Particle trails behind moving objects
4. **Color Grading**: Post-processing for crystal atmosphere

### **Step 3: Performance Optimization**
Ensure the biome doesn't hurt performance:

```csharp
using UnityEngine;

public class CrystalBiomeOptimizer : MonoBehaviour
{
    public float cullingDistance = 50f;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Disable effects when far from camera
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);

        if (distance > cullingDistance)
        {
            // Disable particle systems, complex shaders, etc.
            SetEffectsEnabled(false);
        }
        else
        {
            SetEffectsEnabled(true);
        }
    }

    void SetEffectsEnabled(bool enabled)
    {
        // Enable/disable visual effects based on distance
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            if (enabled)
                ps.Play();
            else
                ps.Stop();
        }
    }
}
```

---

## 🧪 **Phase 5: Integration & Testing (30 minutes)**

### **Step 1: Integrate with MetVanDAMN**
1. Create biome zone in your world
2. Assign CrystalBiomeProfile to the zone
3. Test that effects activate when entering

### **Step 2: Balance Testing**
1. Test slippery movement - not too frustrating?
2. Test crystal growth - satisfying to use?
3. Test enemy AI - challenging but fair?
4. Test performance - smooth at target framerate?

### **Step 3: Player Feedback**
1. Add visual indicators for biome effects
2. Add audio cues for entering/leaving zones
3. Add UI hints for crystal mechanics

### **Step 4: Final Polish**
1. Adjust all values in CrystalBiomeProfile
2. Test different combinations
3. Get feedback from playtesters
4. Iterate on balance and fun factor

---

## 🎉 **Crystal Biome Complete!**

**What you created:**
- ✅ Unique crystal visual theme
- ✅ Slippery movement mechanics
- ✅ Interactive crystal growth system
- ✅ Light-based enemy AI
- ✅ Atmospheric audio system
- ✅ Performance optimizations

### **Biome Expansion Ideas**
- **Crystal Puzzles**: Light refraction challenges
- **Crystal Weapons**: Energy-based combat
- **Crystal Upgrades**: Temporary power boosts
- **Crystal Boss**: Multi-phase crystal guardian

### **Sharing Your Biome**
1. **Package**: Create a Unity package with all assets
2. **Document**: Write setup instructions
3. **Share**: Upload to asset store or GitHub
4. **Community**: Get feedback and improve

### **Advanced Biome Concepts**
- **[Procedural Crystal Generation](procedural-crystals.md)** - Algorithmic crystal placement
- **[Crystal Shader Variants](crystal-shaders.md)** - Different crystal appearances
- **[Multi-Biome Interactions](biome-interactions.md)** - Crystal + other biome combinations

---

*"You just created an entirely new type of world that didn't exist before. That's the magic of procedural generation - infinite possibilities limited only by your imagination!"*

**What biome will you create next? A lava biome? A forest biome? A space biome?**

**🍑 ✨ You're a Biome Master Now! ✨ 🍑**
