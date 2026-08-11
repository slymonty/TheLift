namespace TheLift.CombatCore
{
    public sealed class ImpactResolver
    {
        private readonly ImpactConfig _config;

        public ImpactResolver(ImpactConfig config = null)
        {
            _config = config ?? new ImpactConfig();
        }

        // §4.8 — Location x Awareness x Surface x Orientation x Force. Surface and
        // Orientation are optional: a standing strike has no landing, so they default
        // to a neutral 1x when absent from the context.
        public ImpactResult Resolve(ImpactContext context)
        {
            ZoneProfile zone = _config.GetZoneProfile(context.Zone);
            float awareness = _config.GetAwarenessMultiplier(context.Awareness);
            float surface = context.Surface.HasValue ? _config.GetSurfaceMultiplier(context.Surface.Value) : 1f;
            float orientation = context.Orientation.HasValue ? _config.GetOrientationMultiplier(context.Orientation.Value) : 1f;

            float scalar = awareness * surface * orientation * context.Force;

            float composure = zone.Composure * scalar;
            float balance = zone.Balance * scalar;
            float rattled = zone.Rattled * scalar;

            if (context.Awareness == AwarenessState.Blindside)
            {
                rattled *= _config.BlindsideRattledMultiplier; // doubled, on top of the 2.2x
            }

            if (context.Orientation == LandingOrientation.HeadFirst)
            {
                rattled *= _config.HeadFirstRattledMultiplier; // "heavy Rattled"
            }

            return new ImpactResult
            {
                ComposureDamage = RoundToWhole(composure),
                BalanceDamage = RoundToWhole(balance),
                RattledDamage = RoundToWhole(rattled)
            };
        }

        // §4.8 — Awareness is derived from the angle between the target's facing and
        // the direction to the attacker. Degree thresholds are config-driven placeholders;
        // the bible does not give exact angles.
        public AwarenessState DeriveAwareness(float relativeFacingAngleDegrees, bool targetBraced)
        {
            float angle = System.Math.Abs(relativeFacingAngleDegrees);

            if (angle <= _config.FacingAngleThresholdDegrees)
            {
                return targetBraced ? AwarenessState.FacingBraced : AwarenessState.Facing;
            }
            if (angle <= _config.PeripheralAngleThresholdDegrees)
            {
                return AwarenessState.Peripheral;
            }
            return AwarenessState.Blindside;
        }

        // §4.8 — "Rolled from exit velocity and remaining Balance." More remaining
        // Balance biases toward Feet; more exit velocity biases toward HeadFirst. The
        // caller supplies the Random instance so a run is reproducible from its seed.
        public LandingOrientation RollLandingOrientation(float exitVelocity, float remainingBalance, float maxBalance, System.Random random)
        {
            float balanceFactor = Clamp01(remainingBalance / maxBalance);
            float velocityFactor = Clamp01(exitVelocity / _config.OrientationVelocityReference);

            // 0 (best, Feet) .. 3 (worst, HeadFirst).
            float severity = (1f - balanceFactor) * 3f + velocityFactor;
            float jitter = ((float)random.NextDouble() - 0.5f) * _config.OrientationJitter;
            severity = Clamp(severity + jitter, 0f, 3f);

            int index = (int)System.Math.Round(severity, System.MidpointRounding.AwayFromZero);
            if (index < 0) index = 0;
            if (index > 3) index = 3;

            switch (index)
            {
                case 0: return LandingOrientation.Feet;
                case 1: return LandingOrientation.Side;
                case 2: return LandingOrientation.Back;
                default: return LandingOrientation.HeadFirst;
            }
        }

        private static float RoundToWhole(float value)
        {
            return (float)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
        }

        private static float Clamp01(float value) => Clamp(value, 0f, 1f);

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
