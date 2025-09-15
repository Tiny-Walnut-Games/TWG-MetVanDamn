using System;

namespace TinyWalnutGames.MetVD.Core
    {
    /// <summary>
    /// Elevation layers where a biome can exist. Flagged for multi-select configuration.
    /// </summary>
    [Flags]
    public enum BiomeElevation : ushort
        {
        None = 0,
        Sky = 1 << 0,
        Surface = 1 << 1,
        Subterranean = 1 << 2,
        Core = 1 << 3,
        Underwater = 1 << 4,
        Space = 1 << 5,

        Any = Sky | Surface | Subterranean | Core | Underwater | Space
        }
    }
