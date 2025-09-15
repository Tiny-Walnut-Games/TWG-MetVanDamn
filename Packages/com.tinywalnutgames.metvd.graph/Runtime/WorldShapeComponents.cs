using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Shared;

namespace TinyWalnutGames.MetVD.Graph
    {
    /// <summary>
    /// Tag set when the world shape mask has been generated and is ready for district fitting
    /// </summary>
    public struct WorldShapeReadyTag : IComponentData { }

    /// <summary>
    /// A coarse grid cell indicating whether it's inside the world silhouette
    /// </summary>
    public struct ShapeCell : IBufferElementData
        {
        public int2 Position; // in coarse grid coordinates
        public byte Filled;   // 1 = inside, 0 = outside
        public ShapeCell(int2 pos, byte filled) { Position = pos; Filled = filled; }
        }

    /// <summary>
    /// Parameters to drive the coarse shape WFC
    /// </summary>
    public struct WorldShapeConfig : IComponentData
        {
        public int2 GridSize;     // coarse grid (e.g., 32x18)
        public float FillTarget;  // desired fill ratio [0..1]
        }
    }
