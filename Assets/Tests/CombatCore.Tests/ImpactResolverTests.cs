using NUnit.Framework;

namespace TheLift.CombatCore.Tests
{
    public class ImpactResolverTests
    {
        [Test]
        public void Blindside_Applies2_2x_AndDoublesRattled()
        {
            var config = new ImpactConfig();
            var resolver = new ImpactResolver(config);

            var context = new ImpactContext { Zone = HitZone.Head, Awareness = AwarenessState.Blindside, Force = 1f };
            var result = resolver.Resolve(context);

            float expectedComposure = RoundAwayFromZero(config.Head.Composure * config.BlindsideMultiplier);
            float expectedBalance = RoundAwayFromZero(config.Head.Balance * config.BlindsideMultiplier);
            float expectedRattled = RoundAwayFromZero(config.Head.Rattled * config.BlindsideMultiplier * config.BlindsideRattledMultiplier);

            Assert.AreEqual(expectedComposure, result.ComposureDamage, 0.0001f);
            Assert.AreEqual(expectedBalance, result.BalanceDamage, 0.0001f);
            Assert.AreEqual(expectedRattled, result.RattledDamage, 0.0001f);

            float rattledWithoutDoubling = RoundAwayFromZero(config.Head.Rattled * config.BlindsideMultiplier);
            Assert.Greater(result.RattledDamage, rattledWithoutDoubling);
        }

        [TestCase(LandingSurface.CarpetOrBody, 0.6f)]
        [TestCase(LandingSurface.Concrete, 1.3f)]
        [TestCase(LandingSurface.DeskOrTable, 1.5f)]
        [TestCase(LandingSurface.GlassPartition, 1.4f)]
        [TestCase(LandingSurface.StairFlight, 1.8f)]
        [TestCase(LandingSurface.RailingOrEdge, 1.2f)]
        [TestCase(LandingSurface.ServerRack, 1.6f)]
        [TestCase(LandingSurface.Water, 0.4f)]
        public void SurfaceMultiplier_IsExact(LandingSurface surface, float expected)
        {
            var config = new ImpactConfig();
            Assert.AreEqual(expected, config.GetSurfaceMultiplier(surface), 0.0001f);
        }

        [Test]
        public void HeadFirstLanding_Is2xMultiplier_WithHeavyRattled()
        {
            var config = new ImpactConfig();
            Assert.AreEqual(2.0f, config.GetOrientationMultiplier(LandingOrientation.HeadFirst), 0.0001f);

            var resolver = new ImpactResolver(config);
            var feetContext = new ImpactContext { Zone = HitZone.Head, Awareness = AwarenessState.Facing, Orientation = LandingOrientation.Feet, Force = 1f };
            var headFirstContext = new ImpactContext { Zone = HitZone.Head, Awareness = AwarenessState.Facing, Orientation = LandingOrientation.HeadFirst, Force = 1f };

            var feetResult = resolver.Resolve(feetContext);
            var headFirstResult = resolver.Resolve(headFirstContext);

            float plainOrientationRatio = config.HeadFirstMultiplier / config.FeetMultiplier;
            float actualRattledRatio = headFirstResult.RattledDamage / feetResult.RattledDamage;

            Assert.Greater(actualRattledRatio, plainOrientationRatio);
        }

        [Test]
        public void RollLandingOrientation_FullBalance_LandsBetterThanExhausted()
        {
            var resolver = new ImpactResolver(new ImpactConfig());

            var fullBalanceRoll = resolver.RollLandingOrientation(
                exitVelocity: 0f, remainingBalance: 100f, maxBalance: 100f, random: new System.Random(42));
            var exhaustedRoll = resolver.RollLandingOrientation(
                exitVelocity: 0f, remainingBalance: 0f, maxBalance: 100f, random: new System.Random(42));

            Assert.AreEqual(LandingOrientation.Feet, fullBalanceRoll);
            Assert.AreEqual(LandingOrientation.HeadFirst, exhaustedRoll);
            Assert.Less(OrdinalSeverity(fullBalanceRoll), OrdinalSeverity(exhaustedRoll));
        }

        [TestCase(0f, false, AwarenessState.Facing)]
        [TestCase(0f, true, AwarenessState.FacingBraced)]
        [TestCase(44f, true, AwarenessState.FacingBraced)]
        [TestCase(46f, false, AwarenessState.Peripheral)]
        [TestCase(134f, false, AwarenessState.Peripheral)]
        [TestCase(136f, false, AwarenessState.Blindside)]
        [TestCase(180f, false, AwarenessState.Blindside)]
        [TestCase(-170f, false, AwarenessState.Blindside)]
        public void DeriveAwareness_MatchesAngleThresholds(float angle, bool braced, AwarenessState expected)
        {
            var resolver = new ImpactResolver(new ImpactConfig());
            Assert.AreEqual(expected, resolver.DeriveAwareness(angle, braced));
        }

        [Test]
        public void DeskLampWorkedExample_ProducesRattled53_AndDazed()
        {
            var resolver = new ImpactResolver(new ImpactConfig());

            var context = new ImpactContext
            {
                Zone = HitZone.Head,
                Awareness = AwarenessState.Peripheral,
                Force = 1f
                // No Surface/Orientation set — this is a standing hit, not a fall.
            };

            var result = resolver.Resolve(context);

            Assert.AreEqual(20f, result.ComposureDamage, 0.0001f); // 14 * 1.4 = 19.6 -> 20
            Assert.AreEqual(77f, result.BalanceDamage, 0.0001f);   // 55 * 1.4 = 77
            Assert.AreEqual(53f, result.RattledDamage, 0.0001f);   // 38 * 1.4 = 53.2 -> 53

            var fighter = new Fighter();
            fighter.AddRattled(result.RattledDamage);

            Assert.AreEqual(53f, fighter.Rattled, 0.0001f);
            Assert.AreEqual(RattledState.Dazed, fighter.RattledState);
        }

        private static int OrdinalSeverity(LandingOrientation orientation)
        {
            switch (orientation)
            {
                case LandingOrientation.Feet: return 0;
                case LandingOrientation.Side: return 1;
                case LandingOrientation.Back: return 2;
                default: return 3;
            }
        }

        private static float RoundAwayFromZero(float value)
        {
            return (float)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
        }
    }
}
