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
        [Tooltip("ความหน่วงแกน Y แยกจากแกน X เพื่อให้นุ่มนวลเวลาตัวละครกระโดด ไม่เวียนหัว (ใส่ 0 หรือน้อยกว่าเพื่อใช้ smoothTime เดียวกัน)")]
        [SerializeField] private float smoothTimeY = 0.22f;
        [SerializeField] private bool lookAhead = true;
        [SerializeField] private float lookAheadDistance = 1.5f;

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

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
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

            Vector3 targetPos = target.position + offset;

            // Look-ahead ตามทิศทางการหันของตัวละคร
            if (lookAhead)
            {
                float facingDirection = Mathf.Sign(target.localScale.x);
                targetPos.x += facingDirection * lookAheadDistance;
            }

            // คำนวณ SmoothDamp แยกแกน X และ Y เพื่อความนุ่มนวลเวลาผู้เล่นกระโดด
            float actualSmoothY = smoothTimeY > 0f ? smoothTimeY : smoothTime;
            float smoothX = Mathf.SmoothDamp(transform.position.x, targetPos.x, ref currentVelocityX, smoothTime);
            float smoothY = Mathf.SmoothDamp(transform.position.y, targetPos.y, ref currentVelocityY, actualSmoothY);
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
