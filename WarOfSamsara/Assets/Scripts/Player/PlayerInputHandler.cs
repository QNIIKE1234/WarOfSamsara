using UnityEngine;
using WarOfSamsara.Environment;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WarOfSamsara.Player
{
    /// <summary>
    /// Captures input for player movement, jumping, ladder interactions, and portals.
    /// Supports both Unity's New Input System (UnityEngine.InputSystem) and Legacy Input.
    /// Drives PlayerMovement2D in FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement2D))]
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerMovement2D _movement;

        // Cached input state
        private float _moveInput;
        private float _verticalInput;
        private bool _jumpPressed;
        private bool _jumpHeld;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement2D>();
        }

        private void Update()
        {
            ReadInput();
        }

        private void ReadInput()
        {
            _moveInput = 0f;
            _verticalInput = 0f;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                // Horizontal (Left/Right or A/D)
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) _moveInput -= 1f;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) _moveInput += 1f;

                // Vertical (Up/Down or W/S)
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) _verticalInput -= 1f;
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) _verticalInput += 1f;

                // Jump (Left Alt or Space)
                if (kb.leftAltKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                {
                    _jumpPressed = true;
                }
                _jumpHeld = kb.leftAltKey.isPressed || kb.spaceKey.isPressed;

                // Dash (Left Shift or Right Shift)
                if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
                {
                    _movement.TriggerDash();
                }

                // Portal: Up arrow or W
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
                {
                    TryEnterPortal();
                }

                return;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            // Fallback for Legacy Input System
            _moveInput = Input.GetAxisRaw("Horizontal");
            _verticalInput = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.Space))
            {
                _jumpPressed = true;
            }
            _jumpHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.Space);

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                _movement.TriggerDash();
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                TryEnterPortal();
            }
#endif
        }

        private void FixedUpdate()
        {
            _movement.ProcessMovement(_moveInput, _verticalInput, _jumpPressed, _jumpHeld);
            _jumpPressed = false;
        }

        private void TryEnterPortal()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.75f);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<MapPortal>(out var portal))
                {
                    if (portal.TryActivatePortal())
                    {
                        break;
                    }
                }
            }
        }
    }
}
