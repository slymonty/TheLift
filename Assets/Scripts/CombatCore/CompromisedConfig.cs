namespace TheLift.CombatCore
{
    public sealed class CompromisedConfig
    {
        public int FramesPerSecond = 60;

        // §4.9 — Snag: bible-exact cost and Balance damage.
        public float SnagStaminaCost = 15f;
        public float SnagBalanceDamage = 60f;

        // "Sprinting targets take it worst" — no exact multiplier is given. Placeholder.
        public float SnagSprintingMultiplier = 1.5f;

        // §4.9 — Cling: bible-exact base rate, escalation tiers, and re-Cling window.
        public float[] ClingRatesPerSecond = { 8f, 12f, 18f };
        public int ClingChainWindowFrames = 600; // 10s
        public int ClothingTearFrames = 360; // ~6s

        // §4.9 — Drag down: bible-exact cost. The target's resulting Prone duration
        // reuses §4.8's "prone ~2s" figure since §4.9 gives no distinct number.
        public float DragDownStaminaCost = 22f;
        public int DragDownProneFrames = 120; // ~2s

        // §4.9 — Post up: bible-exact cost and recovery reduction.
        public float PostUpStaminaCost = 10f;
        public float PostUpRecoveryReductionFraction = 0.3f;

        // §4.9 — Stomp-off: bible-exact costs and the teammate's 0.5s duration. The
        // self duration is "slower" with no exact number given — placeholder, double
        // the teammate's. Balance/Rattled amounts are not given either — placeholders,
        // deliberately modest ("removing an obstruction, not attacking").
        public float StompOffTeammateStaminaCost = 12f;
        public int StompOffTeammateDurationFrames = 30; // 0.5s
        public float StompOffSelfStaminaCost = 15f;
        public int StompOffSelfDurationFrames = 60;
        public float StompOffBalanceDamage = 20f;
        public float StompOffRattledDamage = 10f;

        // §4.8 — generic knockdown ("Knocked-down players are prone ~2s"), used by
        // KnockDown() callers that don't have a more specific duration of their own.
        public int GenericKnockdownProneFrames = 120;
    }
}
