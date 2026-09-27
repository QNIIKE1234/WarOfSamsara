using UnityEngine;
using MapleMMO.Environment;

namespace MapleMMO.Player
{
    /// <summary>
    /// Captures input for player movement, jumping, ladder interactions, and portals.
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

        [Header("Keys Config")]
        [SerializeField] private KeyCode jumpKey = KeyCode.LeftAlt; // Classic Maple Jump is Alt
        [SerializeField] private KeyCode attackKey = KeyCode.LeftControl; // Classic Maple Attack is Ctrl
        [SerializeField] private KeyCode upKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode downKey = KeyCode.DownArrow;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement2D>();
        }

        private void Update()
        {
            // Horizontal (Left/Right arrow or A/D)
            _moveInput = Input.GetAxisRaw("Horizontal");

            // Vertical (Up/Down arrow or W/S)
            _verticalInput = Input.GetAxisRaw("Vertical");

            // Jump (Space or LeftAlt)
            if (Input.GetKeyDown(jumpKey) || Input.GetKeyDown(KeyCode.Space))
            {
                _jumpPressed = true;
            }

            _jumpHeld = Input.GetKey(jumpKey) || Input.GetKey(KeyCode.Space);

            // Portal interaction: press Up arrow while in front of a portal
            if (Input.GetKeyDown(upKey) || Input.GetKeyDown(KeyCode.W))
            {
                TryEnterPortal();
            }
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
