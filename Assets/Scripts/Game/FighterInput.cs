using TheLift.CombatCore;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace TheLift.Game
{
    public class FighterInput : MonoBehaviour
    {
        [SerializeField] private FighterController fighterController;
        [SerializeField] private Transform opponent;
        [SerializeField] private int controlSlot;

        private const float ArenaBound = 14f;

        private ActionType? _pendingIntent;

        private void Update()
        {
            ReadStrikeInput();
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = ReadMoveInput();
            Move(moveInput);
            FaceOpponent();

            if (_pendingIntent.HasValue)
            {
                fighterController.Fighter.TryStartAction(_pendingIntent.Value);
                _pendingIntent = null;
            }
        }

        private Gamepad GetAssignedGamepad()
        {
            ReadOnlyArray<Gamepad> gamepads = Gamepad.all;
            return controlSlot == 0
                ? (gamepads.Count > 0 ? gamepads[0] : null)
                : (gamepads.Count > 1 ? gamepads[1] : null);
        }

        // CRITICAL: presses are captured as edges here in Update(), not FixedUpdate —
        // wasPressedThisFrame is only reliable once per rendered frame.
        private void ReadStrikeInput()
        {
            Gamepad gamepad = GetAssignedGamepad();

            bool lightPressed;
            bool heavyPressed;

            if (gamepad != null)
            {
                lightPressed = gamepad.buttonWest.wasPressedThisFrame;
                heavyPressed = gamepad.buttonNorth.wasPressedThisFrame;
            }
            else
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null) return;

                if (controlSlot == 0)
                {
                    lightPressed = keyboard.fKey.wasPressedThisFrame;
                    heavyPressed = keyboard.gKey.wasPressedThisFrame;
                }
                else
                {
                    lightPressed = keyboard.uKey.wasPressedThisFrame;
                    heavyPressed = keyboard.oKey.wasPressedThisFrame;
                }
            }

            if (heavyPressed) _pendingIntent = ActionType.Heavy;
            else if (lightPressed) _pendingIntent = ActionType.Light;
        }

        private Vector2 ReadMoveInput()
        {
            Gamepad gamepad = GetAssignedGamepad();

            if (gamepad != null)
            {
                return gamepad.leftStick.ReadValue();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            if (controlSlot == 0)
            {
                float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                return new Vector2(x, y);
            }
            else
            {
                // IJKL: I=+Z, K=-Z, J=-X, L=+X
                float x = (keyboard.lKey.isPressed ? 1f : 0f) - (keyboard.jKey.isPressed ? 1f : 0f);
                float y = (keyboard.iKey.isPressed ? 1f : 0f) - (keyboard.kKey.isPressed ? 1f : 0f);
                return new Vector2(x, y);
            }
        }

        private void Move(Vector2 input)
        {
            Vector3 dir = new Vector3(input.x, 0f, input.y);
            dir = Vector3.ClampMagnitude(dir, 1f);

            float speed = fighterController.Fighter.Body.MoveSpeed;
            Vector3 pos = transform.position + dir * speed * Time.fixedDeltaTime;
            pos.x = Mathf.Clamp(pos.x, -ArenaBound, ArenaBound);
            pos.z = Mathf.Clamp(pos.z, -ArenaBound, ArenaBound);

            transform.position = pos;
        }

        private void FaceOpponent()
        {
            if (opponent == null) return;

            Vector3 toOpponent = opponent.position - transform.position;
            toOpponent.y = 0f;
            if (toOpponent.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(toOpponent.normalized, Vector3.up);
        }
    }
}
