using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Entities;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

namespace TinyWalnutGames.MetVD.Tests
{
    /// <summary>
    /// Tests for MetVanDAMN Polarity system ensuring no enum collisions
    /// and proper bitmask operations for dual-polarity gates
    /// </summary>
    public class PolaritySystemTests
    {
        [Test]
        public void PolarityEnum_NoCollisions_ShouldHaveUniqueValues()
        {
            // Tech occupies highest bit (0x80) by design.
            Assert.AreEqual(0x80, (byte)Polarity.Tech, "Tech should be 1<<7 (0x80)");
            Assert.AreEqual(1, (byte)Polarity.Sun);
            Assert.AreEqual(2, (byte)Polarity.Moon);
            Assert.AreEqual(4, (byte)Polarity.Heat);
            Assert.AreEqual(8, (byte)Polarity.Cold);
            Assert.AreEqual(16, (byte)Polarity.Earth);
            Assert.AreEqual(32, (byte)Polarity.Wind);
            Assert.AreEqual(64, (byte)Polarity.Life);
            Assert.AreEqual(128, (byte)Polarity.Tech);
        }

        [Test]
        public void PolarityAny_ShouldOrAllPoles()
        {
            // Test that Any equals OR of all poles (addresses blocker #2) 
            var expected = Polarity.Sun | Polarity.Moon | Polarity.Heat | Polarity.Cold |
                          Polarity.Earth | Polarity.Wind | Polarity.Life | Polarity.Tech;
            
            Assert.AreEqual(expected, Polarity.Any, "Any should be OR of all individual poles");
            Assert.AreEqual(0xFF, (byte)Polarity.Any, "Any should cover all 8 bits");
        }

        [Test]
        public void PolarityBitmasks_DualPolarityGates_ShouldWorkCorrectly()
        {
            // Test dual-polarity combinations work as expected
            Assert.AreEqual(Polarity.Sun | Polarity.Moon, Polarity.SunMoon);
            Assert.AreEqual(Polarity.Heat | Polarity.Cold, Polarity.HeatCold);
            Assert.AreEqual(Polarity.Earth | Polarity.Wind, Polarity.EarthWind);
            Assert.AreEqual(Polarity.Life | Polarity.Tech, Polarity.LifeTech);
        }

        [Test]
        public void PolarityMatching_SingleVsDualPoles_ShouldMatchCorrectly()
        {
            // Test that single polarities match with dual-polarity combinations
            Assert.IsTrue((Polarity.Sun & Polarity.SunMoon) != 0, "Sun should match SunMoon gate");
            Assert.IsTrue((Polarity.Moon & Polarity.SunMoon) != 0, "Moon should match SunMoon gate");
            Assert.IsFalse((Polarity.Heat & Polarity.SunMoon) != 0, "Heat should not match SunMoon gate");
            
            // Test Any matches everything
            Assert.IsTrue((Polarity.Sun & Polarity.Any) != 0, "Sun should match Any");
            Assert.IsTrue((Polarity.Tech & Polarity.Any) != 0, "Tech should match Any");
            Assert.IsTrue((Polarity.SunMoon & Polarity.Any) != 0, "SunMoon should match Any");
        }

        [Test]
        public void PolarityNone_ShouldBeZero()
        {
            // Test that None polarity is properly zero
            Assert.AreEqual(0, (byte)Polarity.None, "None should be zero value");
            Assert.IsFalse((Polarity.None & Polarity.Any) != 0, "None should not match any polarity");
        }
    }
}
