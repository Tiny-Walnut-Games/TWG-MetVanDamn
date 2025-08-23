using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace TinyWalnutGames.MetVD.Authoring
{
    [CreateAssetMenu(
        fileName = "BiomeArtProfile",
        menuName = "MetVanDAMN/Biome Art Profile",
        order = 0)]
    public class BiomeArtProfile : ScriptableObject
    {
        [Header("Biome Identity")]
        public string biomeName;
        public Color debugColor = Color.white;

        [Header("Tilemap Art")]
        [Tooltip("Floor tile (preferably RuleTile from Unity 2D Tilemap Extras)")]
        public TileBase floorTile;
        
        [Tooltip("Wall tile (preferably RuleTile from Unity 2D Tilemap Extras)")]
        public TileBase wallTile;
        
        [Tooltip("Background tile (preferably RuleTile from Unity 2D Tilemap Extras)")]
        public TileBase backgroundTile;

        [Tooltip("Optional tiles for biome-to-biome transitions.")]
        public TileBase[] transitionTiles;

        [Header("Props")]
        [Tooltip("Prefabs to spawn as props in this biome.")]
        public GameObject[] propPrefabs;

        [Range(0f, 1f), Tooltip("Chance to spawn a prop per eligible tile.")]
        public float propSpawnChance = 0.1f;

        [Tooltip("Names of tilemap layers where props can be placed.")]
        public List<string> allowedPropLayers = new List<string>();

        [Header("Advanced")]
        [Tooltip("Optional sorting layer override for biome visuals.")]
        public string sortingLayerOverride;

        [Tooltip("Optional material override for biome visuals.")]
        public Material materialOverride;
    }
}