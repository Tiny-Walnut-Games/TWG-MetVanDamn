using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TinyWalnutGames.MetVD.Core
{
    public struct SudoCodeSnippet : IComponentData
    {
        public FixedString512Bytes Code;
        public bool RunOnce;
        public bool HasExecuted;
    }
}
