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
        [SerializeField] private float shoveRange = 2.5f;
        [SerializeField] private float giveGroundSpeedMultiplier = 0.5f;

        private const float ArenaBound = 14f;
        private const float ShovePushDistance = 2f;

        private FighterController _opponentController;
        private Camera _mainCamera;

        private ActionType? _pendingIntent;
        private bool _slipPending;
        private bool _shovePending;

        private void Awake()
        {
            if (opponent != null)
            {
                _opponentController = opponent.GetComponent<FighterController>();
            }

            _mainCamera = Camera.main;
        }

        private void Update()
        {
            ReadStrikeInput();
            ReadSlipInput();
            ReadShoveInput();
        }

        private void FixedUpdate()
        {
            UpdateGiveGround();

            Vector2 moveInput = ReadMoveInput();
            Move(moveInput);
            FaceOpponent();

            if (_pendingIntent.HasValue)
            {
                fighterController.Fighter.TryStartAction(_pendingIntent.Value);
                _pendingIntent = null;
            }

            if (_slipPending)
            {
                fighterController.Fighter.TrySlip();
                _slipPending = false;
            }

            if (_shovePending)
            {
                ConsumeShove();
                _shovePending = false;
            }

            UpdateCover();
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

        // CRITICAL: same edge-capture rule as strikes — Slip is a tap, read in Update().
        private void ReadSlipInput()
        {
            Gamepad gamepad = GetAssignedGamepad();

            bool slipPressed;

            if (gamepad != null)
            {
                slipPressed = gamepad.buttonSouth.wasPressedThisFrame;
            }
            else
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null) return;

                slipPressed = controlSlot == 0
                    ? keyboard.cKey.wasPressedThisFrame
                    : keyboard.hKey.wasPressedThisFrame;
            }

            if (slipPressed) _slipPending = true;
        }

        // CRITICAL: same edge-capture rule as strikes/slip — Shove is a tap.
        private void ReadShoveInput()
        {
            Gamepad gamepad = GetAssignedGamepad();

            bool shovePressed;

            if (gamepad != null)
            {
                shovePressed = gamepad.rightShoulder.wasPressedThisFrame;
            }
            else
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null) return;

                shovePressed = controlSlot == 0
                    ? keyboard.rKey.wasPressedThisFrame
                    : keyboard.yKey.wasPressedThisFrame;
            }

            if (shovePressed) _shovePending = true;
        }

        private void ConsumeShove()
        {
            if (_opponentController == null) return;

            float distance = Vector3.Distance(transform.position, opponent.position);
            if (distance > shoveRange) return;

            Fighter myFighter = fighterController.Fighter;
            Fighter opponentFighter = _opponentController.Fighter;

            if (!myFighter.TryShove(opponentFighter)) return;

            Vector3 away = opponent.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) return;

            Vector3 newPos = opponent.position + away.normalized * ShovePushDistance;
            newPos.x = Mathf.Clamp(newPos.x, -ArenaBound, ArenaBound);
            newPos.z = Mathf.Clamp(newPos.z, -ArenaBound, ArenaBound);
            newPos.y = opponent.position.y;

            opponent.position = newPos;
        }

        // Give Ground is a hold, polled in FixedUpdate — CombatCore already blocks
        // attacks while it's active, so only the movement-speed effect lives here.
        private void UpdateGiveGround()
        {
            bool held = IsGiveGroundHeld();

            Fighter fighter = fighterController.Fighter;
            if (held && !fighter.IsGivingGround)
            {
                fighter.StartGivingGround();
            }
            else if (!held && fighter.IsGivingGround)
            {
                fighter.StopGivingGround();
            }
        }

        private bool IsGiveGroundHeld()
        {
            Gamepad gamepad = GetAssignedGamepad();
            if (gamepad != null)
            {
                return gamepad.leftTrigger.isPressed;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return controlSlot == 0 ? keyboard.tKey.isPressed : keyboard.nKey.isPressed;
        }

        // Cover is a hold, so it's polled in FixedUpdate against the fighter's
        // actual IsCovering state, not captured as an edge.
        private void UpdateCover()
        {
            Gamepad gamepad = GetAssignedGamepad();

            bool coverHeld;

            if (gamepad != null)
            {
                coverHeld = gamepad.leftShoulder.isPressed;
            }
            else
            {
                Keyboard keyboard = Keyboard.current;
                coverHeld = keyboard != null &&
                    (controlSlot == 0 ? keyboard.vKey.isPressed : keyboard.pKey.isPressed);
            }

            Fighter fighter = fighterController.Fighter;
            if (coverHeld && !fighter.IsCovering)
            {
                fighter.StartCover();
            }
            else if (!coverHeld && fighter.IsCovering)
            {
                fighter.StopCover();
            }
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

        private Vector3 BuildCameraRelativeDirection(Vector2 input)
        {
            Vector3 camForward = Vector3.forward;
            Vector3 camRight = Vector3.right;

            if (_mainCamera != null)
            {
                camForward = _mainCamera.transform.forward;
                camRight = _mainCamera.transform.right;
            }

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 dir = camForward * input.y + camRight * input.x;
            return Vector3.ClampMagnitude(dir, 1f);
        }

        private void Move(Vector2 input)
        {
            Vector3 dir = BuildCameraRelativeDirection(input);

            float speed = fighterController.Fighter.Body.MoveSpeed;
            if (fighterController.Fighter.IsGivingGround)
            {
                speed *= giveGroundSpeedMultiplier;
            }

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
