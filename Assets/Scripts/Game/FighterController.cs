using TheLift.CombatCore;
using UnityEngine;

namespace TheLift.Game
{
    public class FighterController : MonoBehaviour
    {
        [SerializeField] private Archetype archetype = Archetype.Bruiser;

        public Fighter Fighter { get; private set; }
        public Archetype Archetype => archetype;

        private void Awake()
        {
            Fighter = new Fighter(archetype: archetype);

            // TEMPORARY verification — delete once the HUD exists.
            Debug.Log($"{name}: archetype={archetype}, MaxStamina={Fighter.Body.MaxStamina}, MaxComposure={Fighter.Body.MaxComposure}");
        }
    }
}
