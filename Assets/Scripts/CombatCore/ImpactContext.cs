namespace TheLift.CombatCore
{
    public sealed class ImpactContext
    {
        public HitZone Zone;
        public AwarenessState Awareness;

        // A standing strike has no landing — these stay null unless the target
        // is actually falling/being thrown (§4.8's Surface/Orientation subsections).
        public LandingSurface? Surface;
        public LandingOrientation? Orientation;

        public float Force = 1f;
    }
}
