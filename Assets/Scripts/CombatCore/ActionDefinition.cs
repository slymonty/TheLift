namespace TheLift.CombatCore
{
    public sealed class ActionDefinition
    {
        public ActionType Type;
        public int StartupFrames;
        public int ActiveFrames;
        public int RecoveryFrames;
        public float StaminaCost;
        public float ComposureDamage;
        public float BalanceDamage;
        public float RattledDamage;
        public TargetZone TargetZone;
    }
}
