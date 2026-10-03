using UnityEngine;

namespace WarOfSamsara.CameraControl
{
    /// <summary>
    /// กล้อง 2D ติดตามผู้เล่นสไตล์ MapleStory / SoulSaver
    /// - เคลื่อนที่แบบ SmoothDamp พร้อม Look-Ahead ตามทิศทางการหัน
    /// - ล็อกขอบเขตตาม MapBounds เพื่อไม่ให้กล้องหลุดออกนอกแมพ
    /// - รองรับ Camera Shake เมื่อใช้สกิลหรือโดนโจมตี
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private bool lookAhead = true;
        [SerializeField] private float lookAheadDistance = 1.5f;

        [Header("Anti-Motion Sickness (กันเวียนหัวเวลาโดด)")]
        [Tooltip("เปิดระบบกันเวียนหัว: กล้องจะนิ่งสนิทเวลาผู้เล่นกระโดดบนพื้นเดิม และจะเลื่อนตามเฉพาะตอนขึ้นไปเหยียบชั้นใหม่ (สไตล์ MapleStory)")]
        [SerializeField] private bool antiMotionSickness = true;

        [Tooltip("รอให้เท้าแตะพื้น (Grounded) ก่อนกล้องค่อยเลื่อนตามขึ้นไปที่ชั้นใหม่")]
        [SerializeField] private bool followOnlyWhenGrounded = true;

        [Tooltip("ระยะ Dead Zone แกน Y (เมตร): ถ้ากระโดดขึ้น-ลงในระยะนี้ กล้องจะไม่ขยับตามเลย")]
        [SerializeField] private float verticalDeadZone = 2.2f;

        [Tooltip("ความนุ่มนวลเวลาเลื่อนเปลี่ยนชั้นความสูง (ค่ายิ่งมากยิ่งนุ่มนวล ไม่กระตุก)")]
        [SerializeField] private float verticalGlideSmoothTime = 0.35f;

        [Header("Map Boundary Clamping")]
        [SerializeField] private bool useBounds = true;
        [Tooltip("ล็อกแกน X ไม่ให้กล้องหลุดขอบซ้าย-ขวาของฉาก")]
        [SerializeField] private bool clampX = true;
        [Tooltip("ล็อกเพดานด้านบนของแกน Y ด้วยกรอบ MapBounds (หากเปิดไว้แต่ MapBounds เตี้ย กล้องจะไม่ยอมขึ้นตามที่สูง)")]
        [SerializeField] private bool clampY = false;
        [Tooltip("ล็อกเฉพาะขอบล่างของแมพ เพื่อกันไม่ให้กล้องจมลงใต้พื้นดิน แต่ปล่อยให้กล้องลอยตามผู้เล่นขึ้น Platform สูงๆ ได้อิสระ")]
        [SerializeField] private bool clampMinYOnly = true;
        [SerializeField] private Collider2D boundaryCollider;

        [Header("Camera Reference")]
        private Camera cam;
        private Player.PlayerMovement2D playerMovement;
        private float lastAnchorY;
        private bool hasInitializedAnchor = false;

        private float currentVelocityX;
        private float currentVelocityY;
        private float currentVelocityZ;
        private float shakeTimeRemaining;
        private float shakeMagnitude;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        private void Start()
        {
            TryCachePlayerMovement();
        }

        private void TryCachePlayerMovement()
        {
            if (target != null)
            {
                playerMovement = target.GetComponent<Player.PlayerMovement2D>();
                if (playerMovement == null) playerMovement = target.GetComponentInParent<Player.PlayerMovement2D>();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            playerMovement = null;
            hasInitializedAnchor = false;
            TryCachePlayerMovement();
        }

        public void SetBounds(Collider2D bounds)
        {
            boundaryCollider = bounds;
        }

        public void SetClampY(bool enableClampY, bool enableClampMinYOnly = true)
        {
            clampY = enableClampY;
            clampMinYOnly = enableClampMinYOnly;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (playerMovement == null)
            {
                TryCachePlayerMovement();
            }

            Vector3 rawTargetPos = target.position + offset;
            Vector3 targetPos = rawTargetPos;

            // ระบบ Anti-Motion Sickness สไตล์ MapleStory
            if (antiMotionSickness)
            {
                if (!hasInitializedAnchor)
                {
                    lastAnchorY = rawTargetPos.y;
                    hasInitializedAnchor = true;
                }

                bool isGrounded = false;
                bool isClimbing = false;

                if (playerMovement != null)
                {
                    isGrounded = playerMovement.IsGrounded;
                    isClimbing = playerMovement.CurrentState == Player.PlayerMovementState.Climbing;
                }
                else
                {
                    // Fallback ถ้าไม่ใช่ Player: เช็คจากระยะขยับ
                    isGrounded = Mathf.Abs(rawTargetPos.y - lastAnchorY) < 0.1f;
                }

                if (followOnlyWhenGrounded && playerMovement != null)
                {
                    if (isGrounded || isClimbing)
                    {
                        lastAnchorY = rawTargetPos.y;
                    }
                    else
                    {
                        // ป้องกันหลุดจอกรณีกระโดดสูงผิดปกติ หรือร่วงตกเหวลึก
                        float deltaY = rawTargetPos.y - lastAnchorY;
                        if (deltaY > verticalDeadZone * 1.5f)
                        {
                            lastAnchorY = rawTargetPos.y - (verticalDeadZone * 1.5f);
                        }
                        else if (deltaY < -verticalDeadZone)
                        {
                            lastAnchorY = rawTargetPos.y + verticalDeadZone;
                        }
                    }
                }
                else
                {
                    // โหมด DeadZone บริสุทธิ์ (Hollow Knight / Celeste)
                    float deltaY = rawTargetPos.y - lastAnchorY;
                    if (Mathf.Abs(deltaY) > verticalDeadZone)
                    {
                        lastAnchorY = rawTargetPos.y - Mathf.Sign(deltaY) * verticalDeadZone;
                    }
                }

                targetPos.y = lastAnchorY;
            }
            else
            {
                lastAnchorY = rawTargetPos.y;
            }

            // Look-ahead ตามทิศทางการหันของตัวละคร
            if (lookAhead)
            {
                float facingDirection = Mathf.Sign(target.localScale.x);
                targetPos.x += facingDirection * lookAheadDistance;
            }

            // คำนวณ SmoothDamp (ใช้ความหน่วงนุ่มนวลเป็นพิเศษสำหรับแกน Y เพื่อเปลี่ยนชั้นอย่างสมูท)
            float smoothYTime = antiMotionSickness ? verticalGlideSmoothTime : smoothTime;
            float smoothX = Mathf.SmoothDamp(transform.position.x, targetPos.x, ref currentVelocityX, smoothTime);
            float smoothY = Mathf.SmoothDamp(transform.position.y, targetPos.y, ref currentVelocityY, smoothYTime);
            float smoothZ = Mathf.SmoothDamp(transform.position.z, targetPos.z, ref currentVelocityZ, smoothTime);

            Vector3 smoothPos = new Vector3(smoothX, smoothY, smoothZ);

            // Clamp ขอบเขตแมพ
            if (useBounds && boundaryCollider != null)
            {
                float camHalfHeight = cam.orthographicSize;
                float camHalfWidth = camHalfHeight * cam.aspect;

                Bounds b = boundaryCollider.bounds;

                // ตรวจสอบแกน X
                if (clampX)
                {
                    if (b.size.x >= camHalfWidth * 2f)
                    {
                        smoothPos.x = Mathf.Clamp(smoothPos.x, b.min.x + camHalfWidth, b.max.x - camHalfWidth);
                    }
                    else
                    {
                        smoothPos.x = b.center.x; // กึ่งกลางถ้าแมพแคบกว่าจอ
                    }
                }

                // ตรวจสอบแกน Y
                if (clampY)
                {
                    if (b.size.y >= camHalfHeight * 2f)
                    {
                        smoothPos.y = Mathf.Clamp(smoothPos.y, b.min.y + camHalfHeight, b.max.y - camHalfHeight);
                    }
                    else
                    {
                        smoothPos.y = b.center.y;
                    }
                }
                else if (clampMinYOnly)
                {
                    // ล็อกเฉพาะขอบล่าง (ไม่ให้เห็นใต้พื้นดิน แต่ปล่อยให้กล้องตามขึ้น Platform สูงๆ ได้)
                    float minY = b.min.y + camHalfHeight;
                    if (smoothPos.y < minY)
                    {
                        smoothPos.y = minY;
                    }
                }
            }

            // Camera Shake Effect
            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.deltaTime;
                Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                shakeOffset.z = 0f;
                smoothPos += shakeOffset;
            }

            smoothPos.z = offset.z;
            transform.position = smoothPos;
        }

        /// <summary>
        /// สั่งให้กล้องสั่นเมื่อเกิดการระเบิดหรือคอมโบหนัก
        /// </summary>
        public void TriggerShake(float duration = 0.2f, float magnitude = 0.25f)
        {
            shakeTimeRemaining = duration;
            shakeMagnitude = magnitude;
        }
    }
}
