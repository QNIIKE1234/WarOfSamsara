using System.Collections;
using UnityEngine;
using WarOfSamsara.Shared;

namespace WarOfSamsara.Enemy
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Hit,
        Dead
    }

    /// <summary>
    /// สคริปต์พื้นฐานสำหรับศัตรู/มอนสเตอร์ 2D ทุกตัว (BaseEnemy)
    /// รองรับ:
    /// - โหลดข้อมูลจาก EnemyConfig ScriptableObject
    /// - AI ลาดตระเวน (Patrol) ไม่เดินตกเหว
    /// - AI ตรวจจับผู้เล่นและวิ่งไล่ตาม (Chase)
    /// - ท่าโจมตี (Attack) พร้อมคูลดาวน์
    /// - การชนสร้างดาเมจ (Touch Damage สไตล์ MapleStory)
    /// - ระบบรับดาเมจ (TakeDamage), กระเด็น (Knockback), และตาย (Die)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class BaseEnemy : MonoBehaviour, IDamageable
    {
        [Header("--- การกำหนดค่า (Configuration) ---")]
        [SerializeField] private EnemyConfig config;

        [Header("--- คอมโพเนนต์การแสดงผล (Visuals & Animation) ---")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform visualTransform;

        [Header("--- เลเยอร์สำหรับการตรวจจับ (Layers) ---")]
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask groundLayer;

        [Header("--- จุดตรวจจับพื้นข้างหน้า (Ledge Check) ---")]
        [SerializeField] private Transform ledgeCheckPoint;
        [SerializeField] private float ledgeCheckDistance = 1.0f;

        // สถานะภายใน (Runtime State)
        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;
        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentState == EnemyState.Dead;
        public int FacingDirection { get; private set; } = 1; // 1 = Right, -1 = Left

        private Rigidbody2D _rb;
        private Collider2D _col;
        private Vector3 _spawnPosition;
        private Transform _targetPlayer;
        private float _lastAttackTime = -999f;
        private float _lastDamageTakenTime = -999f;
        private float _idleTimer = 0f;
        private int _patrolDirection = 1;
        private Color _originalColor = Color.white;

        private bool _hasSpeedParam;
        private bool _hasMovingParam;
        private bool _hasAttackParam;
        private bool _hasHitParam;
        private bool _hasDieParam;

        // Hashes สำหรับ Animator Parameter
        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimAttack = Animator.StringToHash("Attack");
        private static readonly int AnimHit = Animator.StringToHash("Hit");
        private static readonly int AnimDie = Animator.StringToHash("Die");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();

            if (visualTransform == null && spriteRenderer != null)
                visualTransform = spriteRenderer.transform;
            if (visualTransform == null)
                visualTransform = transform;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
                _originalColor = spriteRenderer.color;

            _spawnPosition = transform.position;
            CheckAnimatorParameters();
        }

        private void Start()
        {
            InitializeStats();
            CheckAnimatorParameters();
        }

        private void CheckAnimatorParameters()
        {
            if (animator == null) return;
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == AnimSpeed) _hasSpeedParam = true;
                if (p.nameHash == AnimIsMoving) _hasMovingParam = true;
                if (p.nameHash == AnimAttack) _hasAttackParam = true;
                if (p.nameHash == AnimHit) _hasHitParam = true;
                if (p.nameHash == AnimDie) _hasDieParam = true;
            }
        }

        public void InitializeStats()
        {
            if (config != null)
            {
                CurrentHp = config.maxHp;
            }
            else
            {
                CurrentHp = 100;
            }
            CurrentState = EnemyState.Idle;
        }

        private void Update()
        {
            if (IsDead) return;

            FindAndTrackPlayer();
            UpdateAnimationParameters();
        }

        private void FixedUpdate()
        {
            if (IsDead) return;

            // ชะงักเมื่อโดนดาเมจ
            if (CurrentState == EnemyState.Hit)
            {
                if (Time.time > _lastDamageTakenTime + 0.3f)
                {
                    CurrentState = EnemyState.Idle;
                }
                return;
            }

            // ถ้ากำลังโจมตี ให้หยุดเดิน
            if (CurrentState == EnemyState.Attack)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

            // ทำงานตาม State ของ AI
            if (_targetPlayer != null)
            {
                HandleChaseAndAttack();
            }
            else
            {
                HandlePatrol();
            }
        }

        #region --- AI Behavior ---

        private void FindAndTrackPlayer()
        {
            float detectDist = config != null ? config.detectRadius : 6.0f;
            float loseDist = config != null ? config.loseTargetRadius : 9.0f;

            if (_targetPlayer == null)
            {
                // ค้นหาผู้เล่นในระยะ detectRadius
                Collider2D hit = Physics2D.OverlapCircle(transform.position, detectDist, playerLayer);
                if (hit != null)
                {
                    _targetPlayer = hit.transform;
                }
            }
            else
            {
                // ตรวจสอบว่าผู้เล่นหนีห่างเกิน loseTargetRadius หรือไม่
                float dist = Vector2.Distance(transform.position, _targetPlayer.position);
                if (dist > loseDist)
                {
                    _targetPlayer = null;
                    CurrentState = EnemyState.Idle;
                }
            }
        }

        private void HandleChaseAndAttack()
        {
            float atkRange = config != null ? config.attackRange : 1.8f;
            float speed = config != null ? config.moveSpeed : 2.0f;
            float atkCooldown = config != null ? config.attackCooldown : 2.0f;

            float distX = _targetPlayer.position.x - transform.position.x;
            float absDistX = Mathf.Abs(distX);

            // พลิกหน้าเข้าหาผู้เล่นเสมอ
            SetFacingDirection(distX > 0 ? 1 : -1);

            // อยู่ในระยะโจมตี
            if (absDistX <= atkRange)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

                if (Time.time >= _lastAttackTime + atkCooldown)
                {
                    StartCoroutine(PerformAttackRoutine());
                }
                else
                {
                    CurrentState = EnemyState.Idle;
                }
            }
            else
            {
                // อยู่นอกระยะโจมตี -> วิ่งไล่ตาม
                CurrentState = EnemyState.Chase;

                // ตรวจสอบขอบเหวข้างหน้าก่อนก้าวเดิน
                if (HasGroundAhead(FacingDirection))
                {
                    _rb.linearVelocity = new Vector2(FacingDirection * speed, _rb.linearVelocity.y);
                }
                else
                {
                    // ขอบเหว: หยุดเดินไม่ตกเหว
                    _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                    CurrentState = EnemyState.Idle;
                }
            }
        }

        private IEnumerator PerformAttackRoutine()
        {
            CurrentState = EnemyState.Attack;
            _lastAttackTime = Time.time;

            if (animator != null && _hasAttackParam)
            {
                animator.SetTrigger(AnimAttack);
            }

            // รอจังหวะอนิเมชันโจมตีเสร็จสิ้น
            yield return new WaitForSeconds(0.6f);

            if (CurrentState == EnemyState.Attack)
            {
                CurrentState = EnemyState.Idle;
            }
        }

        private void HandlePatrol()
        {
            float patrolRadius = config != null ? config.patrolRadius : 4.0f;
            float speed = config != null ? config.moveSpeed * 0.7f : 1.4f; // เดินลาดตระเวนช้าลงเล็กน้อย
            float waitTime = config != null ? config.idleWaitTime : 1.5f;

            if (CurrentState == EnemyState.Idle)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _idleTimer += Time.fixedDeltaTime;

                if (_idleTimer >= waitTime)
                {
                    _idleTimer = 0f;
                    _patrolDirection *= -1; // สลับทิศทาง
                    CurrentState = EnemyState.Patrol;
                }
                return;
            }

            // ลาดตระเวนเดินซ้ายขวา
            SetFacingDirection(_patrolDirection);

            // ตรวจสอบระยะลาดตระเวนจากจุดเกิด
            float distFromSpawn = transform.position.x - _spawnPosition.x;
            if (distFromSpawn > patrolRadius && _patrolDirection > 0)
            {
                CurrentState = EnemyState.Idle;
                return;
            }
            if (distFromSpawn < -patrolRadius && _patrolDirection < 0)
            {
                CurrentState = EnemyState.Idle;
                return;
            }

            // ตรวจสอบสิ่งกีดขวางหรือขอบเหว
            if (!HasGroundAhead(_patrolDirection))
            {
                CurrentState = EnemyState.Idle;
                return;
            }

            _rb.linearVelocity = new Vector2(_patrolDirection * speed, _rb.linearVelocity.y);
        }

        private bool HasGroundAhead(int direction)
        {
            if (config != null && config.hasFlyingMovement) return true;

            Vector2 origin = (ledgeCheckPoint != null)
                ? (Vector2)ledgeCheckPoint.position
                : (Vector2)transform.position + new Vector2(direction * 0.6f, -0.2f);

            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, ledgeCheckDistance, groundLayer);
            return hit.collider != null;
        }

        private void SetFacingDirection(int dir)
        {
            if (dir == 0) return;
            FacingDirection = dir;

            if (visualTransform != null)
            {
                Vector3 scale = visualTransform.localScale;
                scale.x = Mathf.Abs(scale.x) * FacingDirection;
                visualTransform.localScale = scale;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.flipX = (FacingDirection < 0);
            }
        }

        private void UpdateAnimationParameters()
        {
            if (animator == null) return;

            float currentSpeed = Mathf.Abs(_rb.linearVelocity.x);
            if (_hasSpeedParam) animator.SetFloat(AnimSpeed, currentSpeed);
            if (_hasMovingParam) animator.SetBool(AnimIsMoving, currentSpeed > 0.1f);
        }

        #endregion

        #region --- Damage & Combat (IDamageable) ---

        public void TakeDamage(int rawDamage, Vector2 knockbackDirection)
        {
            if (IsDead) return;

            // ระบบ Invincibility คูลดาวน์หลังโดนตี
            float invincDuration = config != null ? config.invincibilityDuration : 0.25f;
            if (Time.time < _lastDamageTakenTime + invincDuration) return;

            _lastDamageTakenTime = Time.time;

            // คำนวณดาเมจหลังหักลบพลังป้องกัน
            int def = config != null ? config.defense : 0;
            int finalDamage = Mathf.Max(1, rawDamage - def);

            CurrentHp -= finalDamage;
            Debug.Log($"<color=#FFAA00>[Enemy]</color> {name} โดนดาเมจ: -{finalDamage} (HP เหลือ: {CurrentHp}/{config?.maxHp ?? 100})");

            // แอนิเมชันชะงัก & โดนตี
            if (animator != null && _hasHitParam)
            {
                animator.SetTrigger(AnimHit);
            }

            // แรงกระเด็น Knockback
            float knockRes = config != null ? config.knockbackResistance : 0.2f;
            float knockFactor = Mathf.Clamp01(1f - knockRes);
            _rb.linearVelocity = knockbackDirection * knockFactor;

            CurrentState = EnemyState.Hit;

            // กะพริบสีแดง
            StartCoroutine(FlashRedRoutine());

            // ตาย
            if (CurrentHp <= 0)
            {
                Die();
            }
        }

        private IEnumerator FlashRedRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.3f, 0.3f, 1f);
                yield return new WaitForSeconds(0.12f);
                spriteRenderer.color = _originalColor;
            }
        }

        private void Die()
        {
            CurrentState = EnemyState.Dead;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false; // ปิดฟิสิกส์ไม่ให้ชนกับใครอีก

            if (_col != null) _col.enabled = false;

            if (animator != null && _hasDieParam)
            {
                animator.SetTrigger(AnimDie);
            }

            Debug.Log($"<color=#FF0044>[Enemy]</color> {name} พ่ายแพ้! มอบ EXP: {config?.expReward ?? 0}");

            // ทำลาย Object หรือคืน Object Pool หลังแสดงแอนิเมชันจบ
            Destroy(gameObject, 1.2f);
        }

        // ระบบ Touch Damage (เดินชนผู้เล่นแล้วผู้เล่นโดนดาเมจ)
        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleTouchDamage(collision.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleTouchDamage(other.gameObject);
        }

        private void HandleTouchDamage(GameObject targetObj)
        {
            if (IsDead) return;

            IDamageable damageable = targetObj.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                int touchDmg = config != null ? config.touchDamage : 10;
                Vector2 knockDir = (targetObj.transform.position - transform.position).normalized * 5f;
                damageable.TakeDamage(touchDmg, knockDir);
            }
        }

        #endregion

        #region --- Gizmos & Setup ---

        private void OnDrawGizmosSelected()
        {
            Vector3 center = transform.position;

            // รัศมีตรวจจับผู้เล่น (Yellow)
            float detectDist = config != null ? config.detectRadius : 6.0f;
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.3f);
            Gizmos.DrawWireSphere(center, detectDist);

            // ระยะโจมตี (Red)
            float atkRange = config != null ? config.attackRange : 1.8f;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(center, atkRange);

            // ขอบเขตการลาดตระเวน (Cyan Line)
            float patrolDist = config != null ? config.patrolRadius : 4.0f;
            Vector3 spawn = Application.isPlaying ? _spawnPosition : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(spawn + Vector3.left * patrolDist, spawn + Vector3.right * patrolDist);

            // เส้นตรวจจับขอบเหว
            if (ledgeCheckPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(ledgeCheckPoint.position, ledgeCheckPoint.position + Vector3.down * ledgeCheckDistance);
            }
        }

        public void SetConfig(EnemyConfig newConfig)
        {
            config = newConfig;
            InitializeStats();
        }

        #endregion
    }
}
