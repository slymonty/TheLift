using NUnit.Framework;

namespace TheLift.CombatCore.Tests
{
    public class DefenceTests
    {
        [Test]
        public void Slip_BeatsAStrike_WithinItsWindow()
        {
            var attacker = new Fighter();
            var target = new Fighter();

            Assert.IsTrue(target.TrySlip());
            Assert.IsTrue(attacker.TryStartAction(ActionType.Light)); // 6f startup

            for (int frame = 0; frame < 5; frame++)
            {
                attacker.Tick(frame);
                target.Tick(frame);
            }
            Assert.AreEqual(ActionPhase.Startup, attacker.ActionPhase);
            Assert.IsTrue(target.IsSlipping);

            attacker.Tick(5); // attacker's 6th tick -> Active. Target not yet ticked this frame.
            Assert.AreEqual(ActionPhase.Active, attacker.ActionPhase);
            Assert.IsTrue(target.IsSlipping);

            float composureBefore = target.Composure;
            attacker.ResolveHit(target);

            Assert.AreEqual(composureBefore, target.Composure);
        }

        [Test]
        public void Slip_ExpiresAfterWindow_SubsequentStrikeLandsNormally()
        {
            var attacker = new Fighter();
            var target = new Fighter();

            Assert.IsTrue(target.TrySlip());
            for (int frame = 0; frame < 10; frame++) target.Tick(frame); // well past the 6f window
            Assert.IsFalse(target.IsSlipping);

            Assert.IsTrue(attacker.TryStartAction(ActionType.Light));
            for (int frame = 0; frame < attacker.CurrentStartupFrames; frame++) attacker.Tick(frame);
            Assert.AreEqual(ActionPhase.Active, attacker.ActionPhase);

            float composureBefore = target.Composure;
            attacker.ResolveHit(target);

            Assert.Less(target.Composure, composureBefore);
        }

        [Test]
        public void Slip_CostsTwelveStamina()
        {
            var fighter = new Fighter();
            Assert.IsTrue(fighter.TrySlip());
            Assert.AreEqual(100f - 12f, fighter.Stamina, 0.0001f);
        }

        [Test]
        public void Slip_IsUnavailable_WhenFloodedOrGone()
        {
            var flooded = new Fighter();
            flooded.AddAdrenaline(60f);
            Assert.AreEqual(AdrenalineState.Flooded, flooded.AdrenalineState);
            Assert.IsFalse(flooded.TrySlip());

            var gone = new Fighter();
            gone.AddAdrenaline(85f);
            Assert.AreEqual(AdrenalineState.Gone, gone.AdrenalineState);
            Assert.IsFalse(gone.TrySlip());
        }

        [Test]
        public void Cover_ReducesStrikeDamage_ButNeverToZero_AndCostsTenPerHit()
        {
            var actionConfig = new ActionConfig();
            var defenceConfig = new DefenceConfig();
            var attacker = new Fighter();
            var target = new Fighter();

            Assert.IsTrue(target.StartCover());
            Assert.IsTrue(attacker.TryStartAction(ActionType.Heavy));
            for (int frame = 0; frame < attacker.CurrentStartupFrames; frame++) attacker.Tick(frame);
            Assert.AreEqual(ActionPhase.Active, attacker.ActionPhase);

            float composureBefore = target.Composure;
            float balanceBefore = target.Balance;
            float staminaBefore = target.Stamina;

            attacker.ResolveHit(target);

            float expectedComposureDamage = actionConfig.Heavy.ComposureDamage * defenceConfig.CoverDamageMultiplier;
            float expectedBalanceDamage = actionConfig.Heavy.BalanceDamage * defenceConfig.CoverDamageMultiplier;

            Assert.Greater(expectedComposureDamage, 0f); // no invincibility frames anywhere
            Assert.AreEqual(composureBefore - expectedComposureDamage, target.Composure, 0.0001f);
            Assert.AreEqual(balanceBefore - expectedBalanceDamage, target.Balance, 0.0001f);
            Assert.AreEqual(staminaBefore - defenceConfig.CoverStaminaCostPerHit, target.Stamina, 0.0001f);
        }

        [Test]
        public void Shove_CostsFullPrice_AgainstHealthyTarget_AndDamagesBalance()
        {
            var config = new DefenceConfig();
            var attacker = new Fighter();
            var target = new Fighter();

            Assert.IsTrue(attacker.TryShove(target));
            Assert.AreEqual(100f - config.ShoveStaminaCost, attacker.Stamina, 0.0001f);
            Assert.AreEqual(100f - config.ShoveBalanceDamage, target.Balance, 0.0001f);
        }

        [Test]
        public void Shove_IsFree_AgainstExhaustedTarget()
        {
            var attacker = new Fighter();
            var target = new Fighter();
            target.SpendStamina(1000f);
            Assert.IsTrue(target.IsExhausted);

            Assert.IsTrue(attacker.TryShove(target));
            Assert.AreEqual(100f, attacker.Stamina, 0.0001f);
        }

        [Test]
        public void Shove_InsideTieUp_BreaksItForThePayer_AtFullPrice_RegardlessOfPartnersExhaustion()
        {
            var config = new DefenceConfig();
            var a = new Fighter();
            var b = new Fighter();
            b.SpendStamina(1000f); // b is Exhausted
            Assert.IsTrue(b.IsExhausted);

            var tieUp = TieUp.TryStart(a, b, config);
            Assert.IsNotNull(tieUp);

            float aStaminaBefore = a.Stamina;
            float bBalanceBefore = b.Balance;

            Assert.IsTrue(a.TryShove(b));

            // Full price even though the target (b) is Exhausted — the neutral discount
            // does not apply inside a clinch.
            Assert.AreEqual(aStaminaBefore - config.ShoveStaminaCost, a.Stamina, 0.0001f);
            Assert.AreEqual(bBalanceBefore - config.ShoveBalanceDamage, b.Balance, 0.0001f);

            Assert.IsFalse(tieUp.IsActive);
            Assert.IsFalse(a.IsTiedUp);
            Assert.IsFalse(b.IsTiedUp); // the hold simply ends for b too -- no free exit of their own
        }

        [Test]
        public void Shove_InNeutral_IsStillFree_AgainstAnExhaustedTarget()
        {
            var attacker = new Fighter();
            var target = new Fighter();
            target.SpendStamina(1000f);
            Assert.IsTrue(target.IsExhausted);

            Assert.IsTrue(attacker.TryShove(target));
            Assert.AreEqual(100f, attacker.Stamina, 0.0001f);
        }

        [Test]
        public void Shove_ByABystander_DoesNotBreakSomeoneElsesTieUp()
        {
            var config = new DefenceConfig();
            var a = new Fighter();
            var b = new Fighter();
            var bystander = new Fighter();

            var tieUp = TieUp.TryStart(a, b, config);
            Assert.IsNotNull(tieUp);

            Assert.IsTrue(bystander.TryShove(a)); // the shove itself still lands...

            Assert.IsTrue(tieUp.IsActive); // ...but it no longer breaks a's tie-up with b
            Assert.IsTrue(a.IsTiedUp);
        }

        [Test]
        public void Shove_CannotTargetAFighterYouAreNotTiedUpWith_WhileYouAreTiedUp()
        {
            var config = new DefenceConfig();
            var a = new Fighter();
            var b = new Fighter();
            var c = new Fighter();

            var tieUp = TieUp.TryStart(a, b, config);
            Assert.IsNotNull(tieUp);

            Assert.IsFalse(a.TryShove(c)); // a is busy in a tie-up with b, not c
        }

        [Test]
        public void TieUp_DrainsBothParticipants_AtEightPerSecond()
        {
            var config = new DefenceConfig();
            var a = new Fighter();
            var b = new Fighter();
            var tieUp = TieUp.TryStart(a, b, config);
            Assert.IsNotNull(tieUp);

            for (int i = 0; i < 60; i++) tieUp.Tick();

            Assert.AreEqual(100f - 8f, a.Stamina, 0.05f);
            Assert.AreEqual(100f - 8f, b.Stamina, 0.05f);
        }

        [Test]
        public void TieUp_HigherRemainingStaminaWinsTheBreak()
        {
            var config = new DefenceConfig();
            var a = new Fighter();
            var b = new Fighter();
            b.SpendStamina(50f); // b now has less stamina than a

            var tieUp = TieUp.TryStart(a, b, config);
            Assert.IsNotNull(tieUp);

            Assert.IsFalse(tieUp.TryBreakFree(b)); // less stamina, fails
            Assert.IsTrue(tieUp.IsActive);

            Assert.IsTrue(tieUp.TryBreakFree(a)); // more stamina, succeeds
            Assert.IsFalse(tieUp.IsActive);
            Assert.IsFalse(a.IsTiedUp);
            Assert.IsFalse(b.IsTiedUp);
        }

        [Test]
        public void TieUp_EqualStamina_NeitherBreaksFree()
        {
            var config = new DefenceConfig();
            var a = new Fighter();
            var b = new Fighter();
            var tieUp = TieUp.TryStart(a, b, config);

            Assert.IsFalse(tieUp.TryBreakFree(a));
            Assert.IsFalse(tieUp.TryBreakFree(b));
            Assert.IsTrue(tieUp.IsActive);
        }

        [Test]
        public void TieUp_CannotStart_WhenAParticipantIsBusy()
        {
            var config = new DefenceConfig();
            var a = new Fighter();
            var b = new Fighter();
            Assert.IsTrue(a.TryStartAction(ActionType.Light));

            var tieUp = TieUp.TryStart(a, b, config);
            Assert.IsNull(tieUp);
            Assert.IsFalse(b.IsTiedUp);
        }

        [Test]
        public void GiveGround_IsFree_AndBlocksAttacking_UntilStopped()
        {
            var fighter = new Fighter();
            Assert.IsTrue(fighter.StartGivingGround());
            Assert.AreEqual(100f, fighter.Stamina, 0.0001f);

            Assert.IsFalse(fighter.TryStartAction(ActionType.Light));

            fighter.StopGivingGround();
            Assert.IsTrue(fighter.TryStartAction(ActionType.Light));
        }

        [Test]
        public void DefensiveActions_AreBlocked_WhileBusyWithAnotherAction()
        {
            var fighter = new Fighter();
            Assert.IsTrue(fighter.TryStartAction(ActionType.Heavy)); // occupies Startup

            Assert.IsFalse(fighter.TrySlip());
            Assert.IsFalse(fighter.StartCover());
            Assert.IsFalse(fighter.StartGivingGround());
            Assert.IsFalse(fighter.TryShove(new Fighter()));
        }
    }
}
