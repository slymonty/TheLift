namespace TheLift.CombatCore
{
    public sealed class ImpactConfig
    {
        // §4.8 Location table. Only Head is bible-exact, taken from the §4.10 worked
        // example (Rattled +38, Composure -14, Balance -55). The bible gives Jaw/Temple,
        // Torso, and Limb only as qualitative words (Low/Medium/High/etc.) with no
        // numbers, so those are inferred to preserve the table's stated ordering
        // relative to Head (e.g. Jaw/Temple > Head on Balance and Rattled).
        public ZoneProfile Head = new ZoneProfile { Composure = 14f, Balance = 55f, Rattled = 38f };
        public ZoneProfile JawTemple = new ZoneProfile { Composure = 14f, Balance = 70f, Rattled = 55f };
        public ZoneProfile Torso = new ZoneProfile { Composure = 32f, Balance = 30f, Rattled = 0f };

        // Limb Rattled is "Accumulates toward Broken" in the bible — a separate injury
        // mechanic (§4.15) not implemented yet, so it contributes 0 Rattled here.
        public ZoneProfile Limb = new ZoneProfile { Composure = 14f, Balance = 15f, Rattled = 0f };

        // §4.8 Awareness — bible-exact.
        public float FacingBracedMultiplier = 0.7f;
        public float FacingMultiplier = 1.0f;
        public float PeripheralMultiplier = 1.4f;
        public float BlindsideMultiplier = 2.2f;
        public float BlindsideRattledMultiplier = 2.0f; // "with doubled Rattled" — bible-exact

        // Degree thresholds for DeriveAwareness are not given by the bible — placeholders
        // pending playtesting, consistent with §4.11's 6-8m soft-lock ranges being the
        // only spatial numbers actually specified.
        public float FacingAngleThresholdDegrees = 45f;
        public float PeripheralAngleThresholdDegrees = 135f;

        // §4.8 Landing surface — Damage column, bible-exact. The bible's Balance/Rattled
        // columns for surfaces are qualitative descriptions, not independent numbers, and
        // are not modelled as separate multipliers here.
        public float CarpetOrBodyMultiplier = 0.6f;
        public float ConcreteMultiplier = 1.3f;
        public float DeskOrTableMultiplier = 1.5f;
        public float GlassPartitionMultiplier = 1.4f;
        public float StairFlightMultiplier = 1.8f;
        public float RailingOrEdgeMultiplier = 1.2f;
        public float ServerRackMultiplier = 1.6f;
        public float WaterMultiplier = 0.4f;

        // §4.8 Landing orientation — bible-exact.
        public float FeetMultiplier = 0.5f;
        public float SideMultiplier = 1.0f;
        public float BackMultiplier = 1.3f;
        public float HeadFirstMultiplier = 2.0f;

        // "head/neck first ... with heavy Rattled" — qualitative, not a bible number.
        // Placeholder, smaller than Blindside's explicit "doubled" (x2).
        public float HeadFirstRattledMultiplier = 1.5f;

        // RollLandingOrientation tuning — not given by the bible. Placeholders.
        public float OrientationVelocityReference = 12f;
        public float OrientationJitter = 0.3f;

        public ZoneProfile GetZoneProfile(HitZone zone)
        {
            switch (zone)
            {
                case HitZone.Head: return Head;
                case HitZone.JawTemple: return JawTemple;
                case HitZone.Torso: return Torso;
                case HitZone.Limb: return Limb;
                default: throw new System.ArgumentOutOfRangeException(nameof(zone));
            }
        }

        public float GetAwarenessMultiplier(AwarenessState awareness)
        {
            switch (awareness)
            {
                case AwarenessState.FacingBraced: return FacingBracedMultiplier;
                case AwarenessState.Facing: return FacingMultiplier;
                case AwarenessState.Peripheral: return PeripheralMultiplier;
                case AwarenessState.Blindside: return BlindsideMultiplier;
                default: throw new System.ArgumentOutOfRangeException(nameof(awareness));
            }
        }

        public float GetSurfaceMultiplier(LandingSurface surface)
        {
            switch (surface)
            {
                case LandingSurface.CarpetOrBody: return CarpetOrBodyMultiplier;
                case LandingSurface.Concrete: return ConcreteMultiplier;
                case LandingSurface.DeskOrTable: return DeskOrTableMultiplier;
                case LandingSurface.GlassPartition: return GlassPartitionMultiplier;
                case LandingSurface.StairFlight: return StairFlightMultiplier;
                case LandingSurface.RailingOrEdge: return RailingOrEdgeMultiplier;
                case LandingSurface.ServerRack: return ServerRackMultiplier;
                case LandingSurface.Water: return WaterMultiplier;
                default: throw new System.ArgumentOutOfRangeException(nameof(surface));
            }
        }

        public float GetOrientationMultiplier(LandingOrientation orientation)
        {
            switch (orientation)
            {
                case LandingOrientation.Feet: return FeetMultiplier;
                case LandingOrientation.Side: return SideMultiplier;
                case LandingOrientation.Back: return BackMultiplier;
                case LandingOrientation.HeadFirst: return HeadFirstMultiplier;
                default: throw new System.ArgumentOutOfRangeException(nameof(orientation));
            }
        }
    }
}
