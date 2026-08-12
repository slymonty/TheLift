using TheLift.CombatCore;
using UnityEngine;

namespace TheLift.Game
{
    public class CombatDriver : MonoBehaviour
    {
        [SerializeField] private FighterController fighterA;
        [SerializeField] private FighterController fighterB;
        [SerializeField] private int seed = 12345;
        [SerializeField] private float strikeRange = 2.5f;

        private System.Random _random;
        private int _frame;

        private bool _aSwingResolved;
        private bool _bSwingResolved;

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

            ResolveDirection(fighterA, fighterB, ref _aSwingResolved);
            ResolveDirection(fighterB, fighterA, ref _bSwingResolved);

            // TEMPORARY verification — delete once the loop's running is visible another way.
            if (_frame % 60 == 0)
            {
                Debug.Log($"sim second {_frame / 60}");
            }
        }

        // Exactly one ResolveHit per swing: the flag arms while Active and is
        // cleared the moment the attacker leaves Active, so the next swing can land.
        private void ResolveDirection(FighterController attacker, FighterController target, ref bool resolved)
        {
            if (attacker.Fighter.ActionPhase != ActionPhase.Active)
            {
                resolved = false;
                return;
            }

            if (resolved) return;

            float distance = Vector3.Distance(attacker.transform.position, target.transform.position);
            if (distance <= strikeRange)
            {
                attacker.Fighter.ResolveHit(target.Fighter);
                resolved = true;
            }
        }
    }
}
