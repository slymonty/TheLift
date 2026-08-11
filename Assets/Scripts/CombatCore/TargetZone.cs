namespace TheLift.CombatCore
{
    // Placeholder pending the full impact-resolution model (§4.8) — a strike does not
    // carry an intrinsic target zone yet; where a hit lands is resolved elsewhere.
    public enum TargetZone
    {
        Any,
        Head,
        Torso,
        Limb
    }
}
