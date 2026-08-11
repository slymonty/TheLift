namespace TheLift.CombatCore
{
    public sealed class ArchetypeConfig
    {
        // Part VII — bible-exact Composure/Stamina/Regen/Speed/Dmg. Special-rule
        // baselines not pinned to a number by the bible's Part VII table (the §4.7
        // default weak-grapple cost of 12 and reversal window/cost of 8f/30, the §4.15
        // default revive time of 6s, and the §8.2 default carry fraction of 0.6) are
        // carried on every archetype so each one's override reads as a delta against
        // the same baseline everyone else uses.
        public ArchetypeDefinition Heavy = new ArchetypeDefinition
        {
            Type = Archetype.Heavy,
            MaxComposure = 130f,
            MaxStamina = 70f,
            StaminaRegenPerSecond = 9f,
            MoveSpeed = 3.0f,
            DamageModifier = 1.35f,
            BodyCount = 2,
            CanBreakDebrisSolo = true,
            IsOnlyRealThrower = true,
            WeakGrappleStaminaCost = 12f,
            CanSqueezeThrough = false,
            SearchSpeedMultiplier = 1f,
            ReversalWindowFrames = 8,
            ReversalStaminaCost = 30f,
            ReviveDurationSeconds = 6f,
            CarrySpeedFraction = 0.6f,
            CanStabilizeBroken = false
        };

        public ArchetypeDefinition Bruiser = new ArchetypeDefinition
        {
            Type = Archetype.Bruiser,
            MaxComposure = 100f,
            MaxStamina = 100f,
            StaminaRegenPerSecond = 14f,
            MoveSpeed = 3.4f,
            DamageModifier = 1.00f,
            BodyCount = 1,
            CanBreakDebrisSolo = false,
            IsOnlyRealThrower = false,
            WeakGrappleStaminaCost = 12f,
            CanSqueezeThrough = false,
            SearchSpeedMultiplier = 1f,
            ReversalWindowFrames = 8,
            ReversalStaminaCost = 30f,
            ReviveDurationSeconds = 6f,
            CarrySpeedFraction = 0.6f,
            CanStabilizeBroken = false
        };

        public ArchetypeDefinition Scrapper = new ArchetypeDefinition
        {
            Type = Archetype.Scrapper,
            MaxComposure = 75f,
            MaxStamina = 130f,
            StaminaRegenPerSecond = 20f,
            MoveSpeed = 4.0f,
            DamageModifier = 0.80f,
            BodyCount = 1,
            CanBreakDebrisSolo = false,
            IsOnlyRealThrower = false,
            WeakGrappleStaminaCost = 6f, // "Weak grapples cost 6"
            CanSqueezeThrough = false,
            SearchSpeedMultiplier = 1f,
            ReversalWindowFrames = 8,
            ReversalStaminaCost = 30f,
            ReviveDurationSeconds = 6f,
            CarrySpeedFraction = 0.6f,
            CanStabilizeBroken = false
        };

        public ArchetypeDefinition Agile = new ArchetypeDefinition
        {
            Type = Archetype.Agile,
            MaxComposure = 70f,
            MaxStamina = 120f,
            StaminaRegenPerSecond = 22f,
            MoveSpeed = 4.2f,
            DamageModifier = 0.70f,
            BodyCount = 1,
            CanBreakDebrisSolo = false,
            IsOnlyRealThrower = false,
            WeakGrappleStaminaCost = 12f,
            CanSqueezeThrough = true, // "Squeeze-through"
            SearchSpeedMultiplier = 1.3f, // "Searches 30% faster"
            ReversalWindowFrames = 8,
            ReversalStaminaCost = 30f,
            ReviveDurationSeconds = 6f,
            CarrySpeedFraction = 0.6f,
            CanStabilizeBroken = false
        };

        public ArchetypeDefinition Technician = new ArchetypeDefinition
        {
            Type = Archetype.Technician,
            MaxComposure = 95f,
            MaxStamina = 100f,
            StaminaRegenPerSecond = 14f,
            MoveSpeed = 3.4f,
            DamageModifier = 0.90f,
            BodyCount = 1,
            CanBreakDebrisSolo = false,
            IsOnlyRealThrower = false,
            WeakGrappleStaminaCost = 12f,
            CanSqueezeThrough = false,
            SearchSpeedMultiplier = 1f,
            ReversalWindowFrames = 12, // "12f reversal window"
            ReversalStaminaCost = 20f, // "Reversals cost 20"
            ReviveDurationSeconds = 6f,
            CarrySpeedFraction = 0.6f,
            CanStabilizeBroken = false
        };

        public ArchetypeDefinition Medic = new ArchetypeDefinition
        {
            Type = Archetype.Medic,
            MaxComposure = 85f,
            MaxStamina = 110f,
            StaminaRegenPerSecond = 16f,
            MoveSpeed = 3.6f,
            DamageModifier = 0.70f,
            BodyCount = 1,
            CanBreakDebrisSolo = false,
            IsOnlyRealThrower = false,
            WeakGrappleStaminaCost = 12f,
            CanSqueezeThrough = false,
            SearchSpeedMultiplier = 1f,
            ReversalWindowFrames = 8,
            ReversalStaminaCost = 30f,
            ReviveDurationSeconds = 3f, // "Revive 3s"
            CarrySpeedFraction = 0.9f, // "Carry at 90%"
            CanStabilizeBroken = true
        };

        public ArchetypeDefinition GetDefinition(Archetype archetype)
        {
            switch (archetype)
            {
                case Archetype.Heavy: return Heavy;
                case Archetype.Bruiser: return Bruiser;
                case Archetype.Scrapper: return Scrapper;
                case Archetype.Agile: return Agile;
                case Archetype.Technician: return Technician;
                case Archetype.Medic: return Medic;
                default: throw new System.ArgumentOutOfRangeException(nameof(archetype));
            }
        }
    }
}
