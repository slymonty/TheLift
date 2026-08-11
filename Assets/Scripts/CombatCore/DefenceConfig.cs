namespace TheLift.CombatCore
{
    public sealed class DefenceConfig
    {
        // §4.6 — bible-exact.
        public float SlipStaminaCost = 12f;
        public int SlipWindowFrames = 6;

        public float CoverStaminaCostPerHit = 10f;
        public float CoverDamageMultiplier = 0.3f; // "absorbs ~70%" -> 30% gets through

        public float ShoveStaminaCost = 15f;

        // "hits Balance" — no exact number is given. Placeholder between a Light (15)
        // and a Heavy (45) strike's Balance damage.
        public float ShoveBalanceDamage = 20f;

        public float TieUpDrainPerSecond = 8f;
        public int FramesPerSecond = 60;
    }
}
