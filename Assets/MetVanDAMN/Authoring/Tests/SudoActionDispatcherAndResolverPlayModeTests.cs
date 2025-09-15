#if UNITY_2022_3_OR_NEWER
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using TinyWalnutGames.MetVD.Core;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVanDAMN.Authoring;

public class SudoActionDispatcherAndResolverPlayModeTests
{
    private World _world;
    private EntityManager _em;

    [SetUp]
    public void SetUp()
    {
        _world = new World("TestWorld");
        _em = _world.EntityManager;
        DefaultWorldInitialization.Initialize("TestWorld", false);
    }

    [TearDown]
    public void TearDown()
    {
        _world?.Dispose();
    }

    [UnityTest]
    public System.Collections.IEnumerator Dispatcher_Emits_Once_For_OneOff_Hint()
    {
        var hint = _em.CreateEntity(typeof(SudoActionHint));
        _em.SetComponentData(hint, new SudoActionHint
        {
            ActionKey = new FixedString64Bytes("unit_test"),
            OneOff = true,
            Center = float3.zero,
            Radius = 0f,
            ElevationMask = BiomeElevation.Any,
            HasTypeConstraint = 0,
            Seed = 0
        });

        var sys = _world.GetOrCreateSystemManaged<SudoActionDispatcherSystem>();
        sys.Update(_world.Unmanaged);
        yield return null;

        var reqQuery = _em.CreateEntityQuery(typeof(SudoActionRequest));
        Assert.AreEqual(1, reqQuery.CalculateEntityCount(), "Should emit exactly one request");

        // Update again — request should not be re-emitted due to SudoActionDispatched tag
        sys.Update(_world.Unmanaged);
        yield return null;
        Assert.AreEqual(1, reqQuery.CalculateEntityCount(), "Should not emit a second request");
    }

    [UnityTest]
    public System.Collections.IEnumerator Dispatcher_Is_Deterministic_For_Same_Seed_And_Key()
    {
        var hint = _em.CreateEntity(typeof(SudoActionHint));
        var key = new FixedString64Bytes("determinism");
        _em.SetComponentData(hint, new SudoActionHint
        {
            ActionKey = key,
            OneOff = false,
            Center = new float3(10, 0, 5),
            Radius = 3f,
            ElevationMask = BiomeElevation.Any,
            HasTypeConstraint = 0,
            Seed = 1234
        });

        var sys = _world.GetOrCreateSystemManaged<SudoActionDispatcherSystem>();
        sys.Update(_world.Unmanaged);
        yield return null;

        var reqs = _em.CreateEntityQuery(typeof(SudoActionRequest)).ToComponentDataArray<SudoActionRequest>(Allocator.Temp);
        Assert.AreEqual(1, reqs.Length);
        var first = reqs[0];
        reqs.Dispose();

        // Reset: delete prior request, update again, position should match
        foreach (var e in _em.CreateEntityQuery(typeof(SudoActionRequest)).ToEntityArray(Allocator.Temp)) _em.DestroyEntity(e);
        sys.Update(_world.Unmanaged);
        yield return null;

        reqs = _em.CreateEntityQuery(typeof(SudoActionRequest)).ToComponentDataArray<SudoActionRequest>(Allocator.Temp);
        Assert.AreEqual(1, reqs.Length);
        var second = reqs[0];
        reqs.Dispose();

        Assert.AreEqual(first.ResolvedPosition, second.ResolvedPosition, "Positions should be deterministic");
    }

    [UnityTest]
    public System.Collections.IEnumerator Resolver_ORs_Hints_And_Does_Not_Overwrite_Existing_Masks()
    {
        // Two hints of the same type combine via OR
        var h1 = _em.CreateEntity(typeof(BiomeElevationHint));
        _em.SetComponentData(h1, new BiomeElevationHint { Type = BiomeType.SolarPlains, Mask = BiomeElevation.Lowland });
        var h2 = _em.CreateEntity(typeof(BiomeElevationHint));
        _em.SetComponentData(h2, new BiomeElevationHint { Type = BiomeType.SolarPlains, Mask = BiomeElevation.Midland });

        // Biome entity missing mask
        var biome = _em.CreateEntity(typeof(Biome), typeof(NodeId));
        _em.SetComponentData(biome, new Biome(BiomeType.SolarPlains, Polarity.None, 1, Polarity.None, 1));
        _em.SetComponentData(biome, new NodeId { _value = 42 });

        var resolver = _world.GetOrCreateSystemManaged<BiomeElevationResolverSystem>();
        resolver.Update(_world.Unmanaged);
        yield return null;

        Assert.IsTrue(_em.HasComponent<BiomeElevationMask>(biome), "Mask should be added when missing");
        var mask = _em.GetComponentData<BiomeElevationMask>(biome).Mask;
        Assert.AreEqual(BiomeElevation.Lowland | BiomeElevation.Midland, mask);

        // If mask already exists, resolver should not overwrite
        _em.SetComponentData(biome, new BiomeElevationMask(BiomeElevation.Highland));
        resolver.Update(_world.Unmanaged);
        yield return null;
        var mask2 = _em.GetComponentData<BiomeElevationMask>(biome).Mask;
        Assert.AreEqual(BiomeElevation.Highland, mask2, "Existing mask should be preserved");
    }
}
#endif
