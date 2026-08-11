using NUnit.Framework;

namespace TheLift.CombatCore.Tests
{
    public class CompromisedTests
    {
        [Test]
        public void Dazed_CanSnagClingAndPostUp_ButCannotStrike()
        {
            var fighter = new Fighter();
            fighter.AddRattled(50f); // Dazed
            Assert.AreEqual(RattledState.Dazed, fighter.RattledState);

            Assert.IsFalse(fighter.TryStartAction(ActionType.Light));

            var snagTarget = new Fighter();
            Assert.IsTrue(fighter.TrySnag(snagTarget));

            var clingTarget = new Fighter();
            var cling = Cling.TryStart(fighter, clingTarget);
            Assert.IsNotNull(cling);

            Assert.IsTrue(fighter.TryPostUp(nearbyFurniture: true));
        }

        [Test]
        public void Concussed_CanCling_ButCannotSnag_EvenIfAlsoProne()
        {
            var fighter = new Fighter();
            fighter.KnockDown(120); // Prone, which alone would make Snag eligible
            fighter.AddRattled(75f); // ALSO Concussed
            Assert.IsTrue(fighter.IsProne);
            Assert.AreEqual(RattledState.Concussed, fighter.RattledState);

            Assert.IsTrue(fighter.CanCling);
            var clingTarget = new Fighter();
            Assert.IsNotNull(Cling.TryStart(fighter, clingTarget));

            var snagTarget = new Fighter();
            Assert.IsFalse(fighter.TrySnag(snagTarget)); // Concussed overrides Prone eligibility

            Assert.IsFalse(fighter.TryStartAction(ActionType.Light));
        }

        [Test]
        public void Prone_CanSnagAndDragDown_ButCannotCling_OrStrike()
        {
            var fighter = new Fighter();
            fighter.KnockDown(120);
            Assert.IsTrue(fighter.IsProne);
            Assert.AreEqual(RattledState.Fine, fighter.RattledState);

            Assert.IsFalse(fighter.TryStartAction(ActionType.Light));

            var snagTarget = new Fighter();
            Assert.IsTrue(fighter.TrySnag(snagTarget));

            var dragTarget = new Fighter();
            Assert.IsTrue(fighter.TryDragDown(dragTarget));
            Assert.IsTrue(dragTarget.IsProne);

            Assert.IsFalse(fighter.CanCling);
            var clingTarget = new Fighter();
            Assert.IsNull(Cling.TryStart(fighter, clingTarget));
        }

        [Test]
        public void Downed_Cling_DrainsComposure_NotStamina()
        {
            var clinger = new Fighter();
            clinger.AddRattled(95f); // Down
            Assert.AreEqual(RattledState.Down, clinger.RattledState);

            var target = new Fighter();
            var cling = Cling.TryStart(clinger, target);
            Assert.IsNotNull(cling);

            float staminaBefore = clinger.Stamina;
            float composureBefore = clinger.Composure;

            for (int i = 0; i < 60; i++) cling.Tick(); // 1 second

            Assert.AreEqual(staminaBefore, clinger.Stamina, 0.0001f);
            Assert.AreEqual(composureBefore - 8f, clinger.Composure, 0.05f); // base tier, 8/sec
        }

        [Test]
        public void ReCling_CostEscalates_WithinTenSecondWindow_ThenCapsAtEighteen()
        {
            var config = new CompromisedConfig();
            var clinger = new Fighter();
            clinger.AddRattled(50f); // Dazed, eligible

            var cling1 = Cling.TryStart(clinger, new Fighter(), config);
            Assert.AreEqual(0, cling1.RateTierIndex);
            Assert.AreEqual(8f, cling1.RatePerSecond, 0.0001f);
            cling1.End();

            var cling2 = Cling.TryStart(clinger, new Fighter(), config); // re-cling, 0 frames later
            Assert.AreEqual(1, cling2.RateTierIndex);
            Assert.AreEqual(12f, cling2.RatePerSecond, 0.0001f);
            cling2.End();

            var cling3 = Cling.TryStart(clinger, new Fighter(), config);
            Assert.AreEqual(2, cling3.RateTierIndex);
            Assert.AreEqual(18f, cling3.RatePerSecond, 0.0001f);
            cling3.End();

            var cling4 = Cling.TryStart(clinger, new Fighter(), config); // stays capped
            Assert.AreEqual(2, cling4.RateTierIndex);
            Assert.AreEqual(18f, cling4.RatePerSecond, 0.0001f);
        }

        [Test]
        public void ReCling_JustBeforeWindowExpiry_StillEscalated()
        {
            var config = new CompromisedConfig();
            var clinger = new Fighter();
            // High enough that ~10s of natural Rattled decay (~1/sec) can't drop the
            // fighter out of Cling eligibility (Down -> Concussed, both still eligible)
            // while clinger.Tick() runs across the wait loop below.
            clinger.AddRattled(95f);

            var cling1 = Cling.TryStart(clinger, new Fighter(), config);
            cling1.End();

            // Frame-boundary convention: the window is valid on ticks 0..N-1.
            for (int i = 0; i < config.ClingChainWindowFrames - 1; i++) clinger.Tick(i);

            var cling2 = Cling.TryStart(clinger, new Fighter(), config);
            Assert.AreEqual(1, cling2.RateTierIndex);
        }

        [Test]
        public void ReCling_AtWindowExpiry_ResetsToBaseRate()
        {
            var config = new CompromisedConfig();
            var clinger = new Fighter();
            clinger.AddRattled(95f);

            var cling1 = Cling.TryStart(clinger, new Fighter(), config);
            cling1.End();

            // Frame-boundary convention: the window is stale at tick N.
            for (int i = 0; i < config.ClingChainWindowFrames; i++) clinger.Tick(i);

            var cling2 = Cling.TryStart(clinger, new Fighter(), config);
            Assert.AreEqual(0, cling2.RateTierIndex);
        }

        [Test]
        public void ClothingTear_EndsTheHold_AfterSixSeconds()
        {
            var config = new CompromisedConfig();
            var clinger = new Fighter();
            clinger.AddRattled(50f); // Dazed
            var cling = Cling.TryStart(clinger, new Fighter(), config);

            for (int i = 0; i < config.ClothingTearFrames - 1; i++) cling.Tick();
            Assert.IsTrue(cling.IsActive);

            cling.Tick();
            Assert.IsFalse(cling.IsActive);
        }

        [Test]
        public void StompOff_OnlyBreaksTheCling_ClingerCanReGrab()
        {
            var clinger = new Fighter();
            clinger.AddRattled(50f);
            var target = new Fighter();
            var cling = Cling.TryStart(clinger, target);

            Assert.IsTrue(target.TryStompOff(cling, isSelf: true));
            Assert.IsFalse(cling.IsActive);

            var reGrab = Cling.TryStart(clinger, target);
            Assert.IsNotNull(reGrab); // stomp-off doesn't block a re-grab
        }

        [Test]
        public void StompOff_Self_CostsMoreAndTakesLonger_ThanTeammates()
        {
            var config = new CompromisedConfig();
            Assert.Greater(config.StompOffSelfStaminaCost, config.StompOffTeammateStaminaCost);
            Assert.Greater(config.StompOffSelfDurationFrames, config.StompOffTeammateDurationFrames);

            var clinger1 = new Fighter();
            clinger1.AddRattled(50f);
            var target1 = new Fighter();
            var cling1 = Cling.TryStart(clinger1, target1, config);
            float target1StaminaBefore = target1.Stamina;
            Assert.IsTrue(target1.TryStompOff(cling1, isSelf: true));
            Assert.AreEqual(target1StaminaBefore - config.StompOffSelfStaminaCost, target1.Stamina, 0.0001f);

            var clinger2 = new Fighter();
            clinger2.AddRattled(50f);
            var target2 = new Fighter();
            var teammate = new Fighter();
            var cling2 = Cling.TryStart(clinger2, target2, config);
            float teammateStaminaBefore = teammate.Stamina;
            Assert.IsTrue(teammate.TryStompOff(cling2, isSelf: false));
            Assert.AreEqual(teammateStaminaBefore - config.StompOffTeammateStaminaCost, teammate.Stamina, 0.0001f);
        }

        [Test]
        public void SelfStompOff_Unavailable_WhenDazedOrWorse_RequiresATeammate()
        {
            var clinger = new Fighter();
            clinger.AddRattled(50f);
            var target = new Fighter();
            target.AddRattled(50f); // the clung fighter is themselves Dazed
            var cling = Cling.TryStart(clinger, target);

            Assert.IsFalse(target.TryStompOff(cling, isSelf: true));

            var teammate = new Fighter();
            Assert.IsTrue(teammate.TryStompOff(cling, isSelf: false));
        }

        [Test]
        public void StompOff_AddsRattled_ButNoComposureDamage_AndHitsBalance()
        {
            var clinger = new Fighter();
            clinger.AddRattled(50f); // Dazed
            var target = new Fighter();
            var cling = Cling.TryStart(clinger, target);

            float composureBefore = clinger.Composure;
            float rattledBefore = clinger.Rattled;
            float balanceBefore = clinger.Balance;

            Assert.IsTrue(target.TryStompOff(cling, isSelf: true));

            Assert.AreEqual(composureBefore, clinger.Composure, 0.0001f);
            Assert.Greater(clinger.Rattled, rattledBefore);
            Assert.Less(clinger.Balance, balanceBefore);
        }

        [Test]
        public void Snag_DealsSixtyBalanceDamage_AmplifiedWhenTargetIsSprinting()
        {
            var config = new CompromisedConfig();

            var fighter1 = new Fighter();
            fighter1.AddRattled(50f);
            var target1 = new Fighter();
            Assert.IsTrue(fighter1.TrySnag(target1, targetIsSprinting: false));
            Assert.AreEqual(100f - config.SnagBalanceDamage, target1.Balance, 0.0001f);

            var fighter2 = new Fighter();
            fighter2.AddRattled(50f);
            var target2 = new Fighter();
            Assert.IsTrue(fighter2.TrySnag(target2, targetIsSprinting: true));
            Assert.AreEqual(100f - config.SnagBalanceDamage * config.SnagSprintingMultiplier, target2.Balance, 0.0001f);
        }

        [Test]
        public void PostUp_RequiresFurniture_AndReducesProneTimerByThirtyPercent()
        {
            var fighter = new Fighter();
            fighter.KnockDown(100);
            fighter.AddRattled(50f); // Dazed

            Assert.IsFalse(fighter.TryPostUp(nearbyFurniture: false));
            Assert.AreEqual(100, fighter.ProneFramesRemaining);

            Assert.IsTrue(fighter.TryPostUp(nearbyFurniture: true));
            Assert.AreEqual(70, fighter.ProneFramesRemaining);
        }

        [Test]
        public void Exhausted_CountsAsCompromised_AndCanCling()
        {
            var fighter = new Fighter();
            fighter.SpendStamina(1000f);
            Assert.IsTrue(fighter.IsExhausted);
            Assert.AreEqual(RattledState.Fine, fighter.RattledState);

            Assert.IsTrue(fighter.CanCling);
            var cling = Cling.TryStart(fighter, new Fighter());
            Assert.IsNotNull(cling);
        }
    }
}
