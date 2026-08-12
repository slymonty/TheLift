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
        }
    }
}
