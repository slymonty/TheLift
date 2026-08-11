namespace TheLift.CombatCore
{
    // §4.6 — "TIE-UP — 8/sec. Both draining; higher remaining stamina wins the break."
    // A shared, paired state between two fighters. Nothing on Fighter auto-ticks this —
    // the caller (Simulation layer) owns calling Tick() once per frame, since the drain
    // must apply exactly once per frame across both participants, not once per fighter.
    public sealed class TieUp
    {
        private readonly DefenceConfig _config;

        public Fighter FighterA { get; }
        public Fighter FighterB { get; }
        public bool IsActive { get; private set; } = true;

        private TieUp(Fighter fighterA, Fighter fighterB, DefenceConfig config)
        {
            FighterA = fighterA;
            FighterB = fighterB;
            _config = config;
        }

        public static TieUp TryStart(Fighter fighterA, Fighter fighterB, DefenceConfig config = null)
        {
            if (fighterA == null || fighterB == null || fighterA == fighterB) return null;

            config ??= new DefenceConfig();
            if (!fighterA.CanEnterTieUp() || !fighterB.CanEnterTieUp()) return null;

            var tieUp = new TieUp(fighterA, fighterB, config);
            fighterA.EnterTieUp(tieUp);
            fighterB.EnterTieUp(tieUp);
            return tieUp;
        }

        public void Tick()
        {
            if (!IsActive) return;

            float drainPerFrame = _config.TieUpDrainPerSecond / _config.FramesPerSecond;
            FighterA.SpendStamina(drainPerFrame);
            FighterB.SpendStamina(drainPerFrame);
        }

        public bool TryBreakFree(Fighter attempter)
        {
            if (!IsActive) return false;
            if (attempter != FighterA && attempter != FighterB) return false;

            Fighter other = attempter == FighterA ? FighterB : FighterA;
            if (attempter.Stamina <= other.Stamina) return false;

            End();
            return true;
        }

        public void Break()
        {
            End();
        }

        public bool Involves(Fighter fighter) => fighter == FighterA || fighter == FighterB;

        private void End()
        {
            IsActive = false;
            FighterA.ExitTieUp();
            FighterB.ExitTieUp();
        }
    }
}
