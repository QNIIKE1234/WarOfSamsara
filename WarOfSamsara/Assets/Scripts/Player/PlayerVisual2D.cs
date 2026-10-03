using UnityEngine;

namespace WarOfSamsara.Player
{
    /// <summary>
    /// ควบคุมการแสดงผล Sprite และ Animator ของตัวละคร 2D แบบ Frame-by-Frame (FBF)
    /// รับ Event จาก PlayerMovement2D เพื่อสลับท่าทางและพลิกซ้าย-ขวา
    /// </summary>
    [RequireComponent(typeof(PlayerMovement2D))]
    public class PlayerVisual2D : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform visualTransform;

        private PlayerMovement2D _movement;
        private Rigidbody2D _rb;

        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimVertVel = Animator.StringToHash("VerticalVelocity");
        private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsDashing = Animator.StringToHash("IsDashing");
        private static readonly int AnimIsCrouching = Animator.StringToHash("IsCrouching");
        private static readonly int AnimIsClimbing = Animator.StringToHash("IsClimbing");
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimSkill = Animator.StringToHash("Skill");
        private static readonly int AnimHit = Animator.StringToHash("Hit");
        private static readonly int AnimDie = Animator.StringToHash("Die");

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement2D>();
            _rb = GetComponent<Rigidbody2D>();
            if (visualTransform == null && spriteRenderer != null)
            {
                visualTransform = spriteRenderer.transform;
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            if (_movement != null)
            {
                _movement.OnFacingChanged += HandleFacingChanged;
                _movement.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_movement != null)
            {
                _movement.OnFacingChanged -= HandleFacingChanged;
                _movement.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            if (animator == null || _movement == null || _rb == null) return;

            float speed = Mathf.Abs(_rb.linearVelocity.x);
            bool isMoving = speed > 0.1f && _movement.CurrentState == PlayerMovementState.Walking;

            animator.SetFloat(AnimSpeed, speed);
            animator.SetFloat(AnimVertVel, _rb.linearVelocity.y);
            animator.SetBool(AnimIsMoving, isMoving);
            animator.SetBool(AnimIsGrounded, _movement.IsGrounded);
            animator.SetBool(AnimIsDashing, _movement.IsDashing);
            animator.SetBool(AnimIsCrouching, _movement.CurrentState == PlayerMovementState.Crouching);
            animator.SetBool(AnimIsClimbing, _movement.CurrentState == PlayerMovementState.Climbing);
        }

        private void HandleFacingChanged(int facing)
        {
            if (visualTransform != null)
            {
                Vector3 scale = visualTransform.localScale;
                scale.x = Mathf.Abs(scale.x) * facing;
                visualTransform.localScale = scale;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.flipX = (facing < 0);
            }
        }

        private void HandleStateChanged(PlayerMovementState state)
        {
            // State-based triggers handled via Animator parameters
        }

        public void PlayAttack()
        {
            if (animator != null) animator.SetTrigger(AnimAttack);
        }

        public void PlaySkill()
        {
            if (animator != null) animator.SetTrigger(AnimSkill);
        }

        public void PlayHit()
        {
            if (animator != null) animator.SetTrigger(AnimHit);
        }

        public void PlayDie()
        {
            if (animator != null) animator.SetTrigger(AnimDie);
        }

        public void SetupComponents(Animator anim, SpriteRenderer sr, Transform vTrans)
        {
            animator = anim;
            spriteRenderer = sr;
            visualTransform = vTrans;
        }
    }
}
