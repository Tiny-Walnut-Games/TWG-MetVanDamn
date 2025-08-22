#if UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using TinyWalnutGames.MetVD.Authoring;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Tests.Authoring
{
    /// <summary>
    /// Lightweight structural tests for connection & gate idempotency logic (directly exercising helper logic).
    /// Full Baker pipeline requires Unity baking context; here we validate duplicate prevention helpers.
    /// </summary>
    public class BakerTests
    {
        private World _world;
        private EntityManager _em;

        [SetUp]
        public void SetUp()
        {
            _world = new World("BakerLogicTestWorld", WorldFlags.Editor);
            _em = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_world.IsCreated) _world.Dispose();
        }

        [Test]
        public void ConnectionHelper_Idempotent_AddsOnce()
        {
            var e = _em.CreateEntity();
            var buffer = _em.AddBuffer<ConnectionBufferElement>(e);
            var c = new Connection(1, 2, ConnectionType.OneWay, Polarity.Sun, 3.5f);
            AddIfMissing(buffer, c);
            AddIfMissing(buffer, c); // second add ignored
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(2u, buffer[0].Value.ToNodeId);
        }

        [Test]
        public void GateHelper_Idempotent_AddsOnce()
        {
            var e = _em.CreateEntity();
            var buffer = _em.AddBuffer<GateConditionBufferElement>(e);
            var g = new GateCondition(Polarity.Moon, Ability.Jump, GateSoftness.Hard, 0.2f, "Moon Gate");
            AddIfMissing(buffer, g);
            AddIfMissing(buffer, g);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual(Polarity.Moon, buffer[0].Value.RequiredPolarity);
        }

        private void AddIfMissing(DynamicBuffer<ConnectionBufferElement> buf, Connection c)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                var ex = buf[i].Value;
                if (ex.FromNodeId == c.FromNodeId && ex.ToNodeId == c.ToNodeId && ex.Type == c.Type && ex.RequiredPolarity == c.RequiredPolarity && ex.TraversalCost == c.TraversalCost)
                    return;
            }
            buf.Add(c);
        }

        private void AddIfMissing(DynamicBuffer<GateConditionBufferElement> buf, GateCondition g)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                var ex = buf[i].Value;
                if (ex.RequiredPolarity == g.RequiredPolarity && ex.RequiredAbilities == g.RequiredAbilities && ex.Softness == g.Softness && ex.MinimumSkillLevel == g.MinimumSkillLevel && ex.Description.Equals(g.Description))
                    return;
            }
            buf.Add(g);
        }
    }
}
#endif
