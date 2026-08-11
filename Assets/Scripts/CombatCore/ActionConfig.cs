namespace TheLift.CombatCore
{
    public sealed class ActionConfig
    {
        // §4.5 — Strikes. Frame data, Composure/Balance/Stamina exact from the bible table.
        public ActionDefinition Light = new ActionDefinition
        {
            Type = ActionType.Light,
            StartupFrames = 6,
            ActiveFrames = 3,
            RecoveryFrames = 12,
            StaminaCost = 8f,
            ComposureDamage = 8f,
            BalanceDamage = 15f,
            RattledDamage = 0f,
            TargetZone = TargetZone.Any
        };

        public ActionDefinition Heavy = new ActionDefinition
        {
            Type = ActionType.Heavy,
            StartupFrames = 14,
            ActiveFrames = 4,
            RecoveryFrames = 22,
            StaminaCost = 18f,
            ComposureDamage = 22f,
            BalanceDamage = 45f,
            RattledDamage = 0f,
            TargetZone = TargetZone.Any
        };

        public ActionDefinition WeaponLight = new ActionDefinition
        {
            Type = ActionType.WeaponLight,
            StartupFrames = 8,
            ActiveFrames = 3,
            RecoveryFrames = 14,
            StaminaCost = 10f,
            ComposureDamage = 14f,
            BalanceDamage = 25f,
            RattledDamage = 0f,
            TargetZone = TargetZone.Any
        };

        public ActionDefinition WeaponHeavy = new ActionDefinition
        {
            Type = ActionType.WeaponHeavy,
            StartupFrames = 18,
            ActiveFrames = 5,
            RecoveryFrames = 28,
            StaminaCost = 22f,
            ComposureDamage = 34f,
            BalanceDamage = 70f,
            RattledDamage = 0f,
            TargetZone = TargetZone.Any
        };

        // Not given by §4.5 — placeholder tuning pending playtesting.
        // Chain is hard-capped at 2 lights; a third within the window is rejected,
        // never slowed. Window is measured from the end of the previous light's
        // Recovery, not from its Startup.
        public int LightComboStartupBonusFrames = 4;
        public int LightComboWindowFrames = 30;

        public int HeavyStaggerFrames = 18;

        public ActionDefinition GetDefinition(ActionType type)
        {
            switch (type)
            {
                case ActionType.Light: return Light;
                case ActionType.Heavy: return Heavy;
                case ActionType.WeaponLight: return WeaponLight;
                case ActionType.WeaponHeavy: return WeaponHeavy;
                default: throw new System.ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
