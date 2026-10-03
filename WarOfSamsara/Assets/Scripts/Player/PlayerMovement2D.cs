using System;
using UnityEngine;
using WarOfSamsara.Environment;

namespace WarOfSamsara.Player
{
    public enum PlayerMovementState
    {
        Idle,
        Walking,
        Jumping,
        Falling,
        Crouching,
        Climbing
    }

    /// <summary>
    /// Faithful 2D platformer movement controller tailored for classic MapleStory physics:
    /// - Smooth horizontal movement with acceleration/deceleration
    /// - Variable height jumping
    /// - Down + Jump to drop through one-way platforms
    /// - Down to crouch / prone
    /// - Up / Down to grab and climb ladders and ropes
    /// - Jump off ladders with Left/Right + Jump
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movement Stats")]
        [Tooltip("Standard walking speed (Maple default base speed is 100%).")]
        [SerializeField] private float moveSpeed = 6.0f;
        [SerializeField] private float acceleration = 40.0f;
        [SerializeField] private float deceleration = 35.0f;

        [Header("Dash & Martial Arts (SoulSaver)")]
        [SerializeField] private float dashSpeed = 15.0f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.45f;

        [Header("Jumping & Gravity")]
        [SerializeField] private float jumpForce = 12.0f;
        [SerializeField] private float fallGravityMultiplier = 1.8f;
        [SerializeField] private float lowJumpMultiplier = 2.0f;
        [SerializeField] private float terminalFallVelocity = -18.0f;

        [Header("Ladder & Rope Climbing")]
        [SerializeField] private float climbSpeed = 3.5f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask oneWayPlatformLayer;
        [SerializeField] private float groundCheckDistance = 0.12f;

        [Header("Crouch Settings")]
        [SerializeField] private Vector2 normalColliderSize = new Vector2(1.0f, 1.0f);
        [SerializeField] private Vector2 normalColliderOffset = Vector2.zero;
        [SerializeField] private Vector2 crouchColliderSize = new Vector2(1.0f, 0.6f);
        [SerializeField] private Vector2 crouchColliderOffset = new Vector2(0f, -0.2f);

        // Components
        private Rigidbody2D _rb;
        private BoxCollider2D _collider;

        // States
        public PlayerMovementState CurrentState { get; private set; } = PlayerMovementState.Idle;
        public int FacingDirection { get; private set; } = 1; // 1 = Right, -1 = Left
        public bool IsGrounded { get; private set; }
        public bool IsOnOneWayPlatform { get; private set; }
        public bool IsDashing { get; private set; }

        private float _lastDashTime = -999f;

        // Ladder / Rope Tracking
        private ClimbableRope _currentRope;
        private bool _canGrabRope;

        // One-way platform tracking
        private OneWayPlatform _currentPlatform;

        // Events
        public event Action<PlayerMovementState> OnStateChanged;
        public event Action<int> OnFacingChanged;

        private float _defaultGravityScale;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();

            _defaultGravityScale = _rb.gravityScale;
            if (_collider != null)
            {
                normalColliderSize = _collider.size;
                normalColliderOffset = _collider.offset;
            }
        }

        public void TriggerDash()
        {
            if (IsDashing || Time.time < _lastDashTime + dashCooldown) return;
            if (CurrentState == PlayerMovementState.Climbing) return;

            StartCoroutine(PerformDash());
        }

        private System.Collections.IEnumerator PerformDash()
        {
            IsDashing = true;
            _lastDashTime = Time.time;
            float originalGravity = _rb.gravityScale;
            _rb.gravityScale = 0f;

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                _rb.linearVelocity = new Vector2(FacingDirection * dashSpeed, 0f);
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            _rb.gravityScale = originalGravity;
            IsDashing = false;
        }

        /// <summary>
        /// Main physics update called every tick (compatible with standard FixedUpdate or Fusion FixedUpdateNetwork).
        /// </summary>
        public void ProcessMovement(float moveInput, float verticalInput, bool jumpPressed, bool jumpHeld)
        {
            CheckGround();

            if (IsDashing) return;

            switch (CurrentState)
            {
                case PlayerMovementState.Climbing:
                    UpdateClimbing(moveInput, verticalInput, jumpPressed);
                    break;

                default:
                    UpdateStandardMovement(moveInput, verticalInput, jumpPressed, jumpHeld);
                    break;
            }
        }

        #region Standard Platformer Movement

        private void UpdateStandardMovement(float moveInput, float verticalInput, bool jumpPressed, bool jumpHeld)
        {
            // 1. Check for Ladder/Rope grab
            if (_canGrabRope && _currentRope != null)
            {
                if ((verticalInput > 0.5f && transform.position.y < _currentRope.TopY) ||
                    (verticalInput < -0.5f && !IsGrounded))
                {
                    StartClimbing();
                    return;
                }
            }

            // 2. Check for Crouch (Down arrow while grounded)
            if (IsGrounded && verticalInput < -0.5f && Mathf.Abs(moveInput) < 0.1f)
            {
                // Down + Jump = Drop through one-way platform
                if (jumpPressed && IsOnOneWayPlatform && _currentPlatform != null)
                {
                    _currentPlatform.DropThrough(_collider);
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -2f);
                    ChangeState(PlayerMovementState.Falling);
                    return;
                }

                // Normal Crouch
                ChangeState(PlayerMovementState.Crouching);
                SetColliderDimensions(true);
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

            // Exit Crouch if was crouching
            if (CurrentState == PlayerMovementState.Crouching)
            {
                SetColliderDimensions(false);
            }

            // 3. Horizontal Movement
            float targetSpeed = moveInput * moveSpeed;
            float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            float newX = Mathf.MoveTowards(_rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(newX, _rb.linearVelocity.y);

            // Update facing direction
            if (moveInput > 0.05f && FacingDirection != 1)
            {
                FacingDirection = 1;
                OnFacingChanged?.Invoke(1);
            }
            else if (moveInput < -0.05f && FacingDirection != -1)
            {
                FacingDirection = -1;
                OnFacingChanged?.Invoke(-1);
            }

            // 4. Jump
            if (jumpPressed && IsGrounded)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                ChangeState(PlayerMovementState.Jumping);
            }

            // 5. Better Jump Physics (Fall gravity & low jump cut)
            if (_rb.linearVelocity.y < 0)
            {
                _rb.gravityScale = _defaultGravityScale * fallGravityMultiplier;
            }
            else if (_rb.linearVelocity.y > 0 && !jumpHeld)
            {
                _rb.gravityScale = _defaultGravityScale * lowJumpMultiplier;
            }
            else
            {
                _rb.gravityScale = _defaultGravityScale;
            }

            // Terminal fall velocity clamp
            if (_rb.linearVelocity.y < terminalFallVelocity)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, terminalFallVelocity);
            }

            // 6. Update state
            if (IsGrounded)
            {
                ChangeState(Mathf.Abs(_rb.linearVelocity.x) > 0.1f ? PlayerMovementState.Walking : PlayerMovementState.Idle);
            }
            else
            {
                ChangeState(_rb.linearVelocity.y > 0 ? PlayerMovementState.Jumping : PlayerMovementState.Falling);
            }
        }

        #endregion

        #region Ladder & Rope Climbing

        private void StartClimbing()
        {
            ChangeState(PlayerMovementState.Climbing);
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;

            // Snap to rope center X
            transform.position = new Vector3(_currentRope.SnapX, transform.position.y, transform.position.z);
        }

        private void UpdateClimbing(float moveInput, float verticalInput, bool jumpPressed)
        {
            if (_currentRope == null)
            {
                StopClimbing();
                return;
            }

            // Jump off ladder/rope (Left/Right + Jump or just Jump)
            if (jumpPressed)
            {
                StopClimbing();
                int jumpDir = Mathf.Abs(moveInput) > 0.1f ? (int)Mathf.Sign(moveInput) : FacingDirection;
                _rb.linearVelocity = new Vector2(jumpDir * moveSpeed * 0.8f, jumpForce * 0.85f);
                FacingDirection = jumpDir;
                OnFacingChanged?.Invoke(jumpDir);
                return;
            }

            // Climb Up / Down
            float climbVy = verticalInput * climbSpeed;
            _rb.linearVelocity = new Vector2(0f, climbVy);

            // Reached top of ladder
            if (transform.position.y >= _currentRope.TopY && verticalInput > 0)
            {
                transform.position = new Vector3(transform.position.x, _currentRope.TopY + 0.1f, transform.position.z);
                StopClimbing();
                return;
            }

            // Reached bottom of ladder and touching ground
            if (IsGrounded && verticalInput < 0)
            {
                StopClimbing();
                return;
            }
        }

        private void StopClimbing()
        {
            _rb.gravityScale = _defaultGravityScale;
            ChangeState(IsGrounded ? PlayerMovementState.Idle : PlayerMovementState.Falling);
        }

        #endregion

        #region Ground Checking & Triggers

        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[8];

        private void CheckGround()
        {
            if (_collider == null) return;

            // คำนวณตำแหน่งเท้าจาก World Bounds จริง เพื่อให้รองรับทุก Scale อัตโนมัติ
            Bounds bounds = _collider.bounds;
            Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y + 0.05f);
            Vector2 boxSize = new Vector2(bounds.size.x * 0.85f, 0.1f);

            int maskValue = (groundLayer.value != 0 || oneWayPlatformLayer.value != 0)
                ? (groundLayer.value | oneWayPlatformLayer.value)
                : ~0;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(maskValue);
            filter.useTriggers = false;

            int hitCount = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.down, filter, _groundHits, groundCheckDistance);

            Collider2D validGround = null;
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCol = _groundHits[i].collider;
                if (hitCol != null && hitCol != _collider && !hitCol.isTrigger && !hitCol.transform.IsChildOf(transform))
                {
                    validGround = hitCol;
                    break;
                }
            }

            IsGrounded = validGround != null;

            if (IsGrounded)
            {
                IsOnOneWayPlatform = oneWayPlatformLayer.value != 0 && ((1 << validGround.gameObject.layer) & oneWayPlatformLayer.value) != 0;
                _currentPlatform = validGround.GetComponent<OneWayPlatform>();
                if (_currentPlatform != null) IsOnOneWayPlatform = true;
            }
            else
            {
                IsOnOneWayPlatform = false;
                _currentPlatform = null;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<ClimbableRope>(out var rope))
            {
                _canGrabRope = true;
                _currentRope = rope;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent<ClimbableRope>(out var rope) && _currentRope == rope)
            {
                _canGrabRope = false;
                if (CurrentState == PlayerMovementState.Climbing)
                {
                    StopClimbing();
                }
                _currentRope = null;
            }
        }

        #endregion

        #region Helpers

        private void ChangeState(PlayerMovementState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void SetColliderDimensions(bool isCrouching)
        {
            _collider.size = isCrouching ? crouchColliderSize : normalColliderSize;
            _collider.offset = isCrouching ? crouchColliderOffset : normalColliderOffset;
        }

        private void OnDrawGizmosSelected()
        {
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            if (_collider == null) return;

            Bounds bounds = _collider.bounds;
            Vector2 boxCenter = new Vector2(bounds.center.x, bounds.min.y + 0.05f - groundCheckDistance * 0.5f);
            Vector2 boxSize = new Vector2(bounds.size.x * 0.85f, groundCheckDistance + 0.05f);

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(boxCenter, boxSize);
        }

        #endregion
    }
}
