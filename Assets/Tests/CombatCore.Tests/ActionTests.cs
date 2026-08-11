using NUnit.Framework;

namespace TheLift.CombatCore.Tests
{
    public class ActionTests
    {
        [TestCase(ActionType.Light, 6, 3, 12, 8f, 8f, 15f)]
        [TestCase(ActionType.Heavy, 14, 4, 22, 18f, 22f, 45f)]
        [TestCase(ActionType.WeaponLight, 8, 3, 14, 10f, 14f, 25f)]
        [TestCase(ActionType.WeaponHeavy, 18, 5, 28, 22f, 34f, 70f)]
        public void ActionDefinition_MatchesBibleTable(
            ActionType type, int startup, int active, int recovery,
            float staminaCost, float composureDamage, float balanceDamage)
        {
            var config = new ActionConfig();
            var definition = config.GetDefinition(type);

            Assert.AreEqual(startup, definition.StartupFrames);
            Assert.AreEqual(active, definition.ActiveFrames);
            Assert.AreEqual(recovery, definition.RecoveryFrames);
            Assert.AreEqual(staminaCost, definition.StaminaCost);
            Assert.AreEqual(composureDamage, definition.ComposureDamage);
            Assert.AreEqual(balanceDamage, definition.BalanceDamage);
        }

        [Test]
        public void CannotInterruptRecovery()
        {
            var fighter = new Fighter();
            Assert.IsTrue(fighter.TryStartAction(ActionType.Light));

            for (int frame = 0; frame < 9; frame++) // 6 startup + 3 active
            {
                fighter.Tick(frame);
            }
            Assert.AreEqual(ActionPhase.Recovery, fighter.ActionPhase);

            Assert.IsFalse(fighter.TryStartAction(ActionType.Heavy));
            Assert.AreEqual(ActionPhase.Recovery, fighter.ActionPhase);
            Assert.AreEqual(ActionType.Light, fighter.CurrentActionType);
        }

        [Test]
        public void CannotInterruptStartupOrActive()
        {
            var fighter = new Fighter();
            Assert.IsTrue(fighter.TryStartAction(ActionType.Heavy)); // 14f startup
            fighter.Tick(0);
            Assert.AreEqual(ActionPhase.Startup, fighter.ActionPhase);
            Assert.IsFalse(fighter.TryStartAction(ActionType.Light));

            for (int frame = 1; frame < 14; frame++)
            {
                fighter.Tick(frame);
            }
            Assert.AreEqual(ActionPhase.Active, fighter.ActionPhase);
            Assert.IsFalse(fighter.TryStartAction(ActionType.Light));
        }

        [Test]
        public void SecondLight_HasPlusFourStartup_ThenThirdLightIsRejected()
        {
            var config = new ActionConfig();
            var fighter = new Fighter();

            Assert.IsTrue(fighter.TryStartAction(ActionType.Light));
            Assert.AreEqual(config.Light.StartupFrames, fighter.CurrentStartupFrames);

            int firstLightTotalFrames = config.Light.StartupFrames + config.Light.ActiveFrames + config.Light.RecoveryFrames;
            for (int frame = 0; frame < firstLightTotalFrames; frame++)
            {
                fighter.Tick(frame);
            }
            Assert.AreEqual(ActionPhase.Neutral, fighter.ActionPhase);

            Assert.IsTrue(fighter.TryStartAction(ActionType.Light));
            Assert.AreEqual(config.Light.StartupFrames + config.LightComboStartupBonusFrames, fighter.CurrentStartupFrames);

            int secondLightTotalFrames = fighter.CurrentStartupFrames + config.Light.ActiveFrames + config.Light.RecoveryFrames;
            for (int frame = 0; frame < secondLightTotalFrames; frame++)
            {
                fighter.Tick(frame);
            }
            Assert.AreEqual(ActionPhase.Neutral, fighter.ActionPhase);

            Assert.IsFalse(fighter.TryStartAction(ActionType.Light));
            Assert.AreEqual(ActionPhase.Neutral, fighter.ActionPhase);
        }

        [Test]
        public void LightChain_RejectsThirdWithinWindow_ButResetsOneFramePastWindowExpiry()
        {
            var config = new ActionConfig();
            var fighter = new Fighter();

            fighter.TryStartAction(ActionType.Light); // first
            int firstLightTotalFrames = config.Light.StartupFrames + config.Light.ActiveFrames + config.Light.RecoveryFrames;
            for (int frame = 0; frame < firstLightTotalFrames; frame++) fighter.Tick(frame);
            Assert.AreEqual(ActionPhase.Neutral, fighter.ActionPhase);

            fighter.TryStartAction(ActionType.Light); // second, chain now hard-capped at 2
            int secondLightTotalFrames = fighter.CurrentStartupFrames + config.Light.ActiveFrames + config.Light.RecoveryFrames;
            for (int frame = 0; frame < secondLightTotalFrames; frame++) fighter.Tick(frame);
            Assert.AreEqual(ActionPhase.Neutral, fighter.ActionPhase);

            // The window is measured from the end of the second light's Recovery.
            // At exactly the window length, it has not expired yet: the hard cap
            // still applies and a third light is rejected outright, not slowed.
            for (int frame = 0; frame < config.LightComboWindowFrames; frame++) fighter.Tick(frame);
            Assert.IsFalse(fighter.TryStartAction(ActionType.Light));

            // One frame past the window, the chain is stale: this is a fresh first light.
            fighter.Tick(config.LightComboWindowFrames);
            Assert.IsTrue(fighter.TryStartAction(ActionType.Light));
            Assert.AreEqual(config.Light.StartupFrames, fighter.CurrentStartupFrames);
        }

        [Test]
        public void Stamina_DeductsCorrectly_OnSuccessfulAction()
        {
            var config = new ActionConfig();
            var fighter = new Fighter();

            Assert.IsTrue(fighter.TryStartAction(ActionType.Heavy));
            Assert.AreEqual(100f - config.Heavy.StaminaCost, fighter.Stamina, 0.0001f);
        }

        [Test]
        public void Action_IsRejected_WhenStaminaUnaffordable()
        {
            var fighter = new Fighter();
            fighter.SpendStamina(95f); // Stamina -> 5, Heavy costs 18

            Assert.IsFalse(fighter.TryStartAction(ActionType.Heavy));
            Assert.AreEqual(5f, fighter.Stamina, 0.0001f);
            Assert.AreEqual(ActionPhase.Neutral, fighter.ActionPhase);
        }

        [Test]
        public void HealthyAttacker_DealsFullDamage()
        {
            var config = new ActionConfig();
            var attacker = new Fighter();
            var target = new Fighter();

            Assert.IsFalse(attacker.IsExhausted);
            Assert.IsTrue(attacker.TryStartAction(ActionType.Light));
            for (int frame = 0; frame < attacker.CurrentStartupFrames; frame++) attacker.Tick(frame);
            Assert.AreEqual(ActionPhase.Active, attacker.ActionPhase);

            attacker.ResolveHit(target);

            Assert.AreEqual(100f - config.Light.ComposureDamage, target.Composure, 0.0001f);
            Assert.AreEqual(100f - config.Light.BalanceDamage, target.Balance, 0.0001f);
        }

        [Test]
        public void ExhaustedAttacker_DealsHalfDamage()
        {
            var config = new ActionConfig();
            var attacker = CreateExhaustedButSolventFighter();
            Assert.IsTrue(attacker.IsExhausted);
            Assert.GreaterOrEqual(attacker.Stamina, config.Light.StaminaCost);

            var target = new Fighter();
            Assert.IsTrue(attacker.TryStartAction(ActionType.Light));
            for (int frame = 0; frame < attacker.CurrentStartupFrames; frame++) attacker.Tick(frame);
            Assert.AreEqual(ActionPhase.Active, attacker.ActionPhase);

            attacker.ResolveHit(target);

            Assert.AreEqual(100f - config.Light.ComposureDamage * 0.5f, target.Composure, 0.0001f);
            Assert.AreEqual(100f - config.Light.BalanceDamage * 0.5f, target.Balance, 0.0001f);
        }

        [Test]
        public void HeavyHit_AppliesEighteenFrameStagger()
        {
            var actionConfig = new ActionConfig();
            var attacker = new Fighter();
            var target = new Fighter();

            Assert.IsTrue(attacker.TryStartAction(ActionType.Heavy));
            for (int frame = 0; frame < attacker.CurrentStartupFrames; frame++) attacker.Tick(frame);
            Assert.AreEqual(ActionPhase.Active, attacker.ActionPhase);

            attacker.ResolveHit(target);

            Assert.AreEqual(ActionPhase.Staggered, target.ActionPhase);
            Assert.IsFalse(target.TryStartAction(ActionType.Light));

            for (int frame = 0; frame < actionConfig.HeavyStaggerFrames - 1; frame++)
            {
                target.Tick(frame);
            }
            Assert.AreEqual(ActionPhase.Staggered, target.ActionPhase);

            target.Tick(actionConfig.HeavyStaggerFrames);
            Assert.AreEqual(ActionPhase.Neutral, target.ActionPhase);
        }

        [Test]
        public void ResolveHit_DoesNothing_WhenAttackerNotActive()
        {
            var attacker = new Fighter();
            var target = new Fighter();

            attacker.ResolveHit(target);

            Assert.AreEqual(100f, target.Composure);
            Assert.AreEqual(100f, target.Balance);
        }

        private static Fighter CreateExhaustedButSolventFighter()
        {
            var fighterConfig = new FighterConfig();
            var fighter = new Fighter(fighterConfig);
            fighter.SpendStamina(1000f); // clamps to 0, IsExhausted = true

            for (int frame = 0; frame < fighterConfig.StaminaRegenDelayFrames; frame++)
            {
                fighter.Tick(frame);
            }

            // Regen a modest amount: enough to afford a Light (8) but still under
            // the 25 clear threshold, so IsExhausted stays true (hysteresis).
            for (int frame = 0; frame < 40; frame++)
            {
                fighter.Tick(frame);
            }

            return fighter;
        }
    }
}
