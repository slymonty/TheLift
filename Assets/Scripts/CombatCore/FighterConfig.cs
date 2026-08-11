namespace TheLift.CombatCore
{
    public sealed class FighterConfig
    {
        public int FramesPerSecond = 60;

        public float MaxStamina = 100f;
        public float MaxBalance = 100f;
        public float MaxComposure = 100f;
        public float MaxRattled = 100f;
        public float MaxAdrenaline = 100f;

        // §4.2 / §4.3 — stamina regen pauses after a spend and resumes after this delay.
        public float StaminaRegenPerSecond = 14f;
        public float StaminaRegenDelaySeconds = 1.8f;

        public float BalanceRegenPerSecond = 40f;

        public float RattledRegenPerSecond = 1f;

        // §4.3 — Exhausted triggers at 0 stamina but only clears once stamina reaches this value.
        public float ExhaustedClearThreshold = 25f;

        // §4.4 — Adrenaline: the wave.
        public float AdrenalineHotThreshold = 30f;
        public float AdrenalineFloodedThreshold = 60f;
        public float AdrenalineGoneThreshold = 85f;

        // §4.10 — The Rattled ladder.
        public float RattledShakenThreshold = 25f;
        public float RattledDazedThreshold = 50f;
        public float RattledConcussedThreshold = 75f;
        public float RattledDownThreshold = 95f;

        public int StaminaRegenDelayFrames =>
            (int)System.Math.Round(StaminaRegenDelaySeconds * FramesPerSecond);
    }
}
