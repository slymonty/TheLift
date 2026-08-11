namespace TheLift.CombatCore
{
    // §4.9 — CLING: a paired state. Not auto-ticked by Fighter.Tick() — the caller
    // (Simulation layer) owns calling Tick() once per frame, since the drain must
    // apply exactly once per frame across the pair, not once per fighter.
    public sealed class Cling
    {
        private readonly CompromisedConfig _config;
        private int _framesHeld;

        public Fighter Clinger { get; }
        public Fighter Target { get; }
        public int RateTierIndex { get; }
        public bool IsActive { get; private set; } = true;

        private Cling(Fighter clinger, Fighter target, CompromisedConfig config, int rateTierIndex)
        {
            Clinger = clinger;
            Target = target;
            _config = config;
            RateTierIndex = rateTierIndex;
        }

        // Eligibility (Dazed/Concussed/Down/Exhausted) lives on Fighter.CanCling.
        public static Cling TryStart(Fighter clinger, Fighter target, CompromisedConfig config = null)
        {
            if (clinger == null || target == null || clinger == target) return null;

            config ??= new CompromisedConfig();
            if (!clinger.CanCling) return null;

            int tierIndex = clinger.BeginClingChain(config);
            return new Cling(clinger, target, config, tierIndex);
        }

        public float RatePerSecond => _config.ClingRatesPerSecond[RateTierIndex];

        public void Tick()
        {
            if (!IsActive) return;

            _framesHeld++;

            float drainPerFrame = RatePerSecond / _config.FramesPerSecond;
            if (Clinger.RattledState == RattledState.Down)
            {
                Clinger.DamageComposure(drainPerFrame); // §4.9/§4.10 — Downed Cling costs Composure
            }
            else
            {
                Clinger.SpendStamina(drainPerFrame);
            }

            if (_framesHeld >= _config.ClothingTearFrames)
            {
                End(); // clothing tears — the hold ends on its own
            }
        }

        // Stomp-off (or clothing tear) calls this. The clinger can always re-grab —
        // ending the hold doesn't block that, only the escalating cost discourages it.
        public void End()
        {
            if (!IsActive) return;
            IsActive = false;
            Clinger.EndClingChain();
        }

        public bool Involves(Fighter fighter) => fighter == Clinger || fighter == Target;
    }
}
