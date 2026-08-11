using NUnit.Framework;

namespace TheLift.CombatCore.Tests
{
    public class ArchetypeTests
    {
        [TestCase(Archetype.Heavy, 130f, 70f, 9f, 3.0f, 1.35f)]
        [TestCase(Archetype.Bruiser, 100f, 100f, 14f, 3.4f, 1.00f)]
        [TestCase(Archetype.Scrapper, 75f, 130f, 20f, 4.0f, 0.80f)]
        [TestCase(Archetype.Agile, 70f, 120f, 22f, 4.2f, 0.70f)]
        [TestCase(Archetype.Technician, 95f, 100f, 14f, 3.4f, 0.90f)]
        [TestCase(Archetype.Medic, 85f, 110f, 16f, 3.6f, 0.70f)]
        public void ArchetypeDefinition_MatchesBibleTable(
            Archetype archetype, float composure, float stamina, float regen, float speed, float dmg)
        {
            var config = new ArchetypeConfig();
            var definition = config.GetDefinition(archetype);

            Assert.AreEqual(composure, definition.MaxComposure);
            Assert.AreEqual(stamina, definition.MaxStamina);
            Assert.AreEqual(regen, definition.StaminaRegenPerSecond);
            Assert.AreEqual(speed, definition.MoveSpeed);
            Assert.AreEqual(dmg, definition.DamageModifier, 0.0001f);
        }

        [Test]
        public void DefaultFighter_UsesBruiserArchetype_UnchangedFromPreviousBaseline()
        {
            var fighter = new Fighter();
            Assert.AreEqual(Archetype.Bruiser, fighter.Body.Type);
            Assert.AreEqual(100f, fighter.Stamina);
            Assert.AreEqual(100f, fighter.Composure);
        }

        [Test]
        public void Fighter_DerivesMaxStats_FromArchetype()
        {
            var heavy = new Fighter(archetype: Archetype.Heavy);
            Assert.AreEqual(130f, heavy.Composure);
            Assert.AreEqual(70f, heavy.Stamina);

            var scrapper = new Fighter(archetype: Archetype.Scrapper);
            Assert.AreEqual(75f, scrapper.Composure);
            Assert.AreEqual(130f, scrapper.Stamina);
        }

        [Test]
        public void Technician_HasTwelveFrameReversalWindow_EveryoneElseHasEight()
        {
            var config = new ArchetypeConfig();

            Assert.AreEqual(12, config.Technician.ReversalWindowFrames);
            Assert.AreEqual(8, config.Heavy.ReversalWindowFrames);
            Assert.AreEqual(8, config.Bruiser.ReversalWindowFrames);
            Assert.AreEqual(8, config.Scrapper.ReversalWindowFrames);
            Assert.AreEqual(8, config.Agile.ReversalWindowFrames);
            Assert.AreEqual(8, config.Medic.ReversalWindowFrames);

            Assert.AreEqual(20f, config.Technician.ReversalStaminaCost);
            Assert.AreEqual(30f, config.Bruiser.ReversalStaminaCost);
        }

        [Test]
        public void Scrapper_WeakGrappleCostsSix_EveryoneElseCostsTwelve()
        {
            var config = new ArchetypeConfig();

            Assert.AreEqual(6f, config.Scrapper.WeakGrappleStaminaCost);
            Assert.AreEqual(12f, config.Heavy.WeakGrappleStaminaCost);
            Assert.AreEqual(12f, config.Bruiser.WeakGrappleStaminaCost);
            Assert.AreEqual(12f, config.Agile.WeakGrappleStaminaCost);
            Assert.AreEqual(12f, config.Technician.WeakGrappleStaminaCost);
            Assert.AreEqual(12f, config.Medic.WeakGrappleStaminaCost);
        }

        [Test]
        public void Heavy_CountsAsTwoBodies_AndBreaksDebrisSolo()
        {
            var config = new ArchetypeConfig();

            Assert.AreEqual(2, config.Heavy.BodyCount);
            Assert.IsTrue(config.Heavy.CanBreakDebrisSolo);
            Assert.IsTrue(config.Heavy.IsOnlyRealThrower);

            Assert.AreEqual(1, config.Bruiser.BodyCount);
            Assert.IsFalse(config.Bruiser.CanBreakDebrisSolo);
        }

        [Test]
        public void Agile_CanSqueezeThrough_AndSearchesThirtyPercentFaster()
        {
            var config = new ArchetypeConfig();

            Assert.IsTrue(config.Agile.CanSqueezeThrough);
            Assert.AreEqual(1.3f, config.Agile.SearchSpeedMultiplier, 0.0001f);

            Assert.IsFalse(config.Heavy.CanSqueezeThrough);
            Assert.AreEqual(1f, config.Heavy.SearchSpeedMultiplier, 0.0001f);
        }

        [Test]
        public void Medic_RevivesFaster_CarriesFaster_AndCanStabilizeBroken()
        {
            var config = new ArchetypeConfig();

            Assert.AreEqual(3f, config.Medic.ReviveDurationSeconds);
            Assert.AreEqual(0.9f, config.Medic.CarrySpeedFraction, 0.0001f);
            Assert.IsTrue(config.Medic.CanStabilizeBroken);

            Assert.AreEqual(6f, config.Bruiser.ReviveDurationSeconds);
            Assert.AreEqual(0.6f, config.Bruiser.CarrySpeedFraction, 0.0001f);
            Assert.IsFalse(config.Bruiser.CanStabilizeBroken);
        }

        [Test]
        public void DamageModifier_IsWiredIntoResolveHit()
        {
            var actionConfig = new ActionConfig();
            var heavyAttacker = new Fighter(archetype: Archetype.Heavy);
            var scrapperAttacker = new Fighter(archetype: Archetype.Scrapper);
            var target1 = new Fighter();
            var target2 = new Fighter();

            Assert.IsTrue(heavyAttacker.TryStartAction(ActionType.Light));
            for (int f = 0; f < heavyAttacker.CurrentStartupFrames; f++) heavyAttacker.Tick(f);
            heavyAttacker.ResolveHit(target1);

            Assert.IsTrue(scrapperAttacker.TryStartAction(ActionType.Light));
            for (int f = 0; f < scrapperAttacker.CurrentStartupFrames; f++) scrapperAttacker.Tick(f);
            scrapperAttacker.ResolveHit(target2);

            float heavyDamage = 100f - target1.Composure;
            float scrapperDamage = 100f - target2.Composure;

            Assert.AreEqual(actionConfig.Light.ComposureDamage * 1.35f, heavyDamage, 0.0001f);
            Assert.AreEqual(actionConfig.Light.ComposureDamage * 0.80f, scrapperDamage, 0.0001f);
            Assert.Greater(heavyDamage, scrapperDamage);
        }

        [Test]
        public void HeavyVsScrapperFight_CanBeConstructedAndRun()
        {
            var heavy = new Fighter(archetype: Archetype.Heavy);
            var scrapper = new Fighter(archetype: Archetype.Scrapper);

            Assert.AreEqual(Archetype.Heavy, heavy.Body.Type);
            Assert.AreEqual(Archetype.Scrapper, scrapper.Body.Type);

            Assert.IsTrue(heavy.TryStartAction(ActionType.Heavy));
            for (int f = 0; f < heavy.CurrentStartupFrames; f++)
            {
                heavy.Tick(f);
                scrapper.Tick(f);
            }
            Assert.AreEqual(ActionPhase.Active, heavy.ActionPhase);
            heavy.ResolveHit(scrapper);

            Assert.AreEqual(ActionPhase.Staggered, scrapper.ActionPhase); // Heavy landed -> 18f stagger

            for (int f = 0; f < 40; f++)
            {
                heavy.Tick(f);
                scrapper.Tick(f);
            }
            Assert.AreEqual(ActionPhase.Neutral, heavy.ActionPhase);
            Assert.AreEqual(ActionPhase.Neutral, scrapper.ActionPhase);

            Assert.IsTrue(scrapper.TryStartAction(ActionType.Light));
            for (int f = 0; f < scrapper.CurrentStartupFrames; f++)
            {
                heavy.Tick(f);
                scrapper.Tick(f);
            }
            scrapper.ResolveHit(heavy);

            Assert.Less(heavy.Composure, 130f);
            Assert.Less(scrapper.Composure, 75f);
        }
    }
}
