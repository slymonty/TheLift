using UnityEngine;

namespace TheLift.Game
{
    public class CombatDriver : MonoBehaviour
    {
        [SerializeField] private FighterController fighterA;
        [SerializeField] private FighterController fighterB;
        [SerializeField] private int seed = 12345;

        private System.Random _random;
        private int _frame;

        private void Awake()
        {
            Time.fixedDeltaTime = 1f / 60f;
            _random = new System.Random(seed);
        }

        private void FixedUpdate()
        {
            _frame++;

            fighterA.Fighter.Tick(_frame);
            fighterB.Fighter.Tick(_frame);

            // TEMPORARY verification — delete once the loop's running is visible another way.
            if (_frame % 60 == 0)
            {
                Debug.Log($"sim second {_frame / 60}");
            }
        }
    }
}
