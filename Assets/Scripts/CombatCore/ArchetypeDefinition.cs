namespace TheLift.CombatCore
{
    public sealed class ArchetypeDefinition
    {
        public Archetype Type;

        // Part VII table — bible-exact.
        public float MaxComposure;
        public float MaxStamina;
        public float StaminaRegenPerSecond;
        public float MoveSpeed;
        public float DamageModifier;

        // Special rules. Only DamageModifier (above, wired into Fighter.ResolveHit) and
        // the base stats currently drive real behaviour. The systems these remaining
        // fields would drive — debris, grapples, reversals, search, revive/carry,
        // Broken (§4.15/§8.1/§8.2/§9) — are not implemented in CombatCore yet. They are
        // stored here as the single source of truth for when those systems are built.
        public int BodyCount;
        public bool CanBreakDebrisSolo;
        public bool IsOnlyRealThrower;

        public float WeakGrappleStaminaCost; // §4.7 default 12; Scrapper overrides to 6

        public bool CanSqueezeThrough;
        public float SearchSpeedMultiplier;

        public int ReversalWindowFrames; // §4.7 default 8f; Technician overrides to 12f
        public float ReversalStaminaCost; // §4.7 default 30; Technician overrides to 20

        public float ReviveDurationSeconds; // §4.15 default 6s; Medic overrides to 3s
        public float CarrySpeedFraction; // §8.2 default 0.6; Medic overrides to 0.9
        public bool CanStabilizeBroken;
    }
}
