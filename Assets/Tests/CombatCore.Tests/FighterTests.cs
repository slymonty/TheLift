using NUnit.Framework;

namespace TheLift.CombatCore.Tests
{
    public class FighterTests
    {
        [Test]
        public void Stamina_RegenRespectsDelay_BeforeResuming()
        {
            var config = new FighterConfig();
            var fighter = new Fighter(config);

            fighter.SpendStamina(30f); // 100 -> 70
            int delayFrames = config.StaminaRegenDelayFrames;

            // Frame-boundary convention: a delay of N frames blocks regen on ticks
            // 0..N-1 and regen resumes on tick N. One tick short of N: still no regen.
            for (int frame = 0; frame < delayFrames - 1; frame++)
            {
                fighter.Tick(frame);
            }
            Assert.AreEqual(70f, fighter.Stamina, 0.0001f);

            fighter.Tick(delayFrames - 1); // the Nth tick: regen resumes on this frame

            Assert.Greater(fighter.Stamina, 70f);
        }

        [Test]
        public void Stamina_SpendingResetsTheRegenDelay()
        {
            var config = new FighterConfig();
            var fighter = new Fighter(config);

            fighter.SpendStamina(30f); // 100 -> 70
            for (int frame = 0; frame < config.StaminaRegenDelayFrames; frame++) // regen ticks once, on the last of these
            {
                fighter.Tick(frame);
            }
            float staminaBeforeSecondSpend = fighter.Stamina;
            Assert.Greater(staminaBeforeSecondSpend, 70f);

            fighter.SpendStamina(10f); // resets the delay counter
            fighter.Tick(0);

            Assert.AreEqual(staminaBeforeSecondSpend - 10f, fighter.Stamina, 0.0001f);
        }

        [Test]
        public void Exhausted_TriggersAtZero_AndClearsExactlyAt25()
        {
            var fighter = new Fighter();
            fighter.SpendStamina(1000f); // clamps to 0

            Assert.AreEqual(0f, fighter.Stamina);
            Assert.IsTrue(fighter.IsExhausted);

            int frame = 0;
            int safetyLimit = 100000;
            while (fighter.Stamina < 25f)
            {
                Assert.IsTrue(fighter.IsExhausted, $"Should still be exhausted at stamina {fighter.Stamina}");
                fighter.Tick(frame++);
                Assert.Less(frame, safetyLimit, "Stamina never reached the clear threshold");
            }

            Assert.GreaterOrEqual(fighter.Stamina, 25f);
            Assert.IsFalse(fighter.IsExhausted);
        }

        [TestCase(0f, AdrenalineState.Composed)]
        [TestCase(29.99f, AdrenalineState.Composed)]
        [TestCase(30f, AdrenalineState.Hot)]
        [TestCase(59.99f, AdrenalineState.Hot)]
        [TestCase(60f, AdrenalineState.Flooded)]
        [TestCase(84.99f, AdrenalineState.Flooded)]
        [TestCase(85f, AdrenalineState.Gone)]
        [TestCase(100f, AdrenalineState.Gone)]
        public void AdrenalineState_FiresAtExactThresholds(float value, AdrenalineState expected)
        {
            var fighter = new Fighter();
            fighter.AddAdrenaline(value);

            Assert.AreEqual(expected, fighter.AdrenalineState);
        }

        [TestCase(0f, RattledState.Fine)]
        [TestCase(24.99f, RattledState.Fine)]
        [TestCase(25f, RattledState.Shaken)]
        [TestCase(49.99f, RattledState.Shaken)]
        [TestCase(50f, RattledState.Dazed)]
        [TestCase(74.99f, RattledState.Dazed)]
        [TestCase(75f, RattledState.Concussed)]
        [TestCase(94.99f, RattledState.Concussed)]
        [TestCase(95f, RattledState.Down)]
        [TestCase(100f, RattledState.Down)]
        public void RattledState_FiresAtExactThresholds(float value, RattledState expected)
        {
            var fighter = new Fighter();
            fighter.AddRattled(value);

            Assert.AreEqual(expected, fighter.RattledState);
        }

        [Test]
        public void Composure_NeverRegenerates()
        {
            var fighter = new Fighter();
            fighter.DamageComposure(40f); // 100 -> 60

            for (int frame = 0; frame < 600; frame++)
            {
                fighter.Tick(frame);
            }

            Assert.AreEqual(60f, fighter.Composure, 0.0001f);
        }

        [Test]
        public void Rattled_RegensAtApproximatelyOnePerSecond()
        {
            var fighter = new Fighter();
            fighter.AddRattled(50f);

            for (int frame = 0; frame < 60; frame++)
            {
                fighter.Tick(frame);
            }

            Assert.AreEqual(49f, fighter.Rattled, 0.05f);
        }

        [Test]
        public void Balance_RegensAtApproximatelyFortyPerSecond()
        {
            var config = new FighterConfig();
            var fighter = new Fighter(config);
            fighter.DamageBalance(60f); // 100 -> 40

            for (int frame = 0; frame < 60; frame++)
            {
                fighter.Tick(frame);
            }

            Assert.AreEqual(80f, fighter.Balance, 0.05f);
        }

        [Test]
        public void Rattled_DoesNotResetFromTicking_OnlyDecaysTowardZero()
        {
            var fighter = new Fighter();
            fighter.AddRattled(10f);

            for (int frame = 0; frame < 5; frame++)
            {
                fighter.Tick(frame);
            }

            Assert.Greater(fighter.Rattled, 0f);
            Assert.Less(fighter.Rattled, 10f);
        }
    }
}
