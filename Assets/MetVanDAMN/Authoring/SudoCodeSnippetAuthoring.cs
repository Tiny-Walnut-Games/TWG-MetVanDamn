using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVanDAMN.Authoring
    {
    [DisallowMultipleComponent]
    public sealed class SudoCodeSnippetAuthoring : MonoBehaviour
        {
        [TextArea(5, 12)]
        [Tooltip("Sudo-code commands. Supported: spawn <key> [x y z]; log <message>...")]
        public string Code = "log Snippet active;\nspawn spawn_marker_waypoint 0 0 0";

        [Tooltip("If true, code will only run once per scene load.")]
        public bool RunOnce = true;

        class Baker : Baker<SudoCodeSnippetAuthoring>
            {
            public override void Bake(SudoCodeSnippetAuthoring authoring)
                {
				Entity e = GetEntity(TransformUsageFlags.None);
                var snippet = new SudoCodeSnippet
                    {
                    RunOnce = authoring.RunOnce,
                    HasExecuted = false,
                    // store up to 512 chars
                    Code = new FixedString512Bytes(authoring.Code ?? string.Empty)
                    };
                AddComponent(e, snippet);
                }
            }
        }
    }
