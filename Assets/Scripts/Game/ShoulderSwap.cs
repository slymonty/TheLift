using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace TheLift.Game
{
    public class ShoulderSwap : MonoBehaviour
    {
        [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;

        private void Update()
        {
            if (!WasSwapPressed()) return;

            // CameraSide is 0..1 (left..right) — toggling it re-lerps ShoulderOffset.x
            // between -x and +x inside CinemachineThirdPersonFollow for a smooth swap.
            thirdPersonFollow.CameraSide = thirdPersonFollow.CameraSide >= 0.5f ? 0f : 1f;
        }

        private bool WasSwapPressed()
        {
            ReadOnlyArray<Gamepad> gamepads = Gamepad.all;
            Gamepad gamepad = gamepads.Count > 0 ? gamepads[0] : null;

            if (gamepad != null)
            {
                return gamepad.buttonEast.wasPressedThisFrame;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.qKey.wasPressedThisFrame;
        }
    }
}
